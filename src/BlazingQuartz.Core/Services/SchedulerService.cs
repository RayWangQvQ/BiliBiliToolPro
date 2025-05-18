using System;
using BlazingQuartz.Core.Models;
using BlazingQuartz.Jobs;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;

namespace BlazingQuartz.Core.Services;

public class SchedulerService(ILogger<SchedulerService> logger, ISchedulerFactory schedulerFactory)
    : ISchedulerService
{
    public async IAsyncEnumerable<ScheduleModel> GetAllJobsAsync(ScheduleJobFilter? filter = null)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler();
        IReadOnlyCollection<string> jobGroupNames = await scheduler.GetJobGroupNames();

        foreach (var jobGrp in jobGroupNames)
        {
            if (filter is { IncludeSystemJobs: false } && jobGrp == Constants.SYSTEM_GROUP)
                continue;

            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(jobGrp));

            foreach (var jobKey in jobKeys)
            {
                await foreach (var job in GetScheduleModelsAsync(jobKey))
                {
                    yield return job;
                }
            }
        }
    }

    public async Task<ScheduleModel> GetScheduleModelAsync(ITrigger trigger)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var jobDetail = await scheduler.GetJobDetail(trigger.JobKey);

        return await CreateScheduleModel(jobDetail, trigger);
    }

    public async Task<IReadOnlyCollection<string>> GetJobGroups()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        return (await scheduler.GetJobGroupNames())
            .Where(n => n != Constants.SYSTEM_GROUP)
            .ToList();
    }

    public async Task<IReadOnlyCollection<string>> GetTriggerGroups()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        return (await scheduler.GetTriggerGroupNames())
            .Where(n => n != Constants.SYSTEM_GROUP)
            .ToList();
    }

    public async Task<IList<KeyValuePair<string, int>>> GetScheduledJobSummary()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var executingCount = (await scheduler.GetCurrentlyExecutingJobs()).Count;
        var jobCount = (await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup())).Count;
        var triggerCount = (
            await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup())
        ).Count;
        var sysJobCount = (
            await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(Constants.SYSTEM_GROUP))
        ).Count;
        var sysTriggerCount = (
            await scheduler.GetTriggerKeys(
                GroupMatcher<TriggerKey>.GroupEquals(Constants.SYSTEM_GROUP)
            )
        ).Count;

        return new List<KeyValuePair<string, int>>
        {
            new KeyValuePair<string, int>("Jobs", jobCount),
            new KeyValuePair<string, int>("Triggers", triggerCount),
            new KeyValuePair<string, int>("Executing", executingCount),
            new KeyValuePair<string, int>("System Jobs", sysJobCount),
            new KeyValuePair<string, int>("System Triggers", sysTriggerCount),
        };
    }

    public async Task<SchedulerMetaData> GetMetadataAsync()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        return await scheduler.GetMetaData();
    }

    private async Task<ScheduleModel> CreateScheduleModel(
        IJobDetail? jobDetail,
        ITrigger trigger,
        CancellationToken cancellationToken = default
    )
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var triggerState = (await scheduler.GetTriggerState(trigger.Key));
        var runningTrigger = (await scheduler.GetCurrentlyExecutingJobs(cancellationToken))
            .Where(context => context.Trigger.Equals(trigger))
            .FirstOrDefault();

        return new ScheduleModel
        {
            JobName = jobDetail?.Key.Name,
            JobGroup = jobDetail?.Key.Group ?? "No Group",
            JobType = jobDetail?.JobType.ToString(),
            JobDescription = jobDetail?.Description,
            TriggerName = trigger.Key.Name,
            TriggerGroup = trigger.Key.Group,
            TriggerDescription = trigger.Description,
            TriggerType = trigger.GetTriggerType(),
            TriggerTypeClassName = trigger.GetType().Name,
            NextTriggerTime = trigger.GetNextFireTimeUtc(),
            PreviousTriggerTime = trigger.GetPreviousFireTimeUtc(),
            JobStatus =
                runningTrigger != null
                    ? JobStatus.Running
                    : triggerState switch
                    {
                        TriggerState.Paused => JobStatus.Paused,
                        TriggerState.None => JobStatus.NoTrigger,
                        TriggerState.Error => JobStatus.Error,
                        _ => JobStatus.Idle,
                    },
            TriggerDetail = CreateTriggerDetailModel(trigger),
        };
    }

    public async Task CreateSchedule(
        JobDetailModel jobDetailModel,
        TriggerDetailModel triggerDetailModel
    )
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var trigger = BuildTrigger(triggerDetailModel);

        // Determine if job already exists
        if (await ContainsJobKey(jobDetailModel.Name, jobDetailModel.Group))
        {
            var existingJob = await scheduler.GetJobDetail(
                new JobKey(jobDetailModel.Name, jobDetailModel.Group)
            );
            if (existingJob != null)
            {
                //await scheduler.GetTriggersOfJob(job.Key)
                var jobTriggers = new List<ITrigger>(1);
                jobTriggers.Add(trigger);

                await scheduler.ScheduleJob(existingJob, jobTriggers.AsReadOnly(), true);
                return;
            }
        }

        var job = CreateJobDetail(jobDetailModel);

        await scheduler.ScheduleJob(job, trigger);
    }

    public async Task UpdateSchedule(
        Key oldJobKey,
        Key? oldTriggerKey,
        JobDetailModel newJobModel,
        TriggerDetailModel newTriggerModel
    )
    {
        var scheduler = await schedulerFactory.GetScheduler().ConfigureAwait(false);
        var oJobKey = oldJobKey.ToJobKey();

        var newJob = CreateJobDetail(newJobModel);
        var trigger = BuildTrigger(newTriggerModel, newJob.Key);
        // determine if old triggerKey exists
        if (
            oldTriggerKey != null
            && await scheduler.CheckExists(oldTriggerKey.ToTriggerKey()).ConfigureAwait(false)
        )
        {
            await scheduler.UnscheduleJob(oldTriggerKey.ToTriggerKey()).ConfigureAwait(false);
        }

        var existingTriggers = await scheduler.GetTriggersOfJob(oJobKey).ConfigureAwait(false);

        // assign new job to all triggers
        var triggers = existingTriggers
            .Select(t =>
            {
                var b = t.GetTriggerBuilder().ForJob(newJob.Key);
                if (t.StartTimeUtc < DateTimeOffset.UtcNow)
                    b.StartNow();
                return b.Build();
            })
            .ToList();
        triggers.Add(trigger);

        // delete old job
        await scheduler.DeleteJob(oJobKey).ConfigureAwait(false);

        // save new job with triggers
        await scheduler.ScheduleJob(newJob, triggers, replace: true).ConfigureAwait(false);
    }

    public async Task<JobDetailModel?> GetJobDetail(string jobName, string groupName)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var jd = await scheduler.GetJobDetail(new JobKey(jobName, groupName));

        if (jd == null)
            return null;

        return new JobDetailModel
        {
            Name = jd.Key.Name,
            Group = jd.Key.Group,
            Description = jd.Description,
            JobDataMap = jd.JobDataMap,
            JobClass = jd.JobType,
            IsDurable = jd.Durable,
        };
    }

    public async Task<TriggerDetailModel?> GetTriggerDetail(string triggerName, string triggerGroup)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var trigger = await scheduler.GetTrigger(new TriggerKey(triggerName, triggerGroup));

        if (trigger == null)
            return null;

        return CreateTriggerDetailModel(trigger);
    }

    public async Task<bool> ContainsTriggerKey(string triggerName, string triggerGroup)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        return await scheduler.CheckExists(new TriggerKey(triggerName, triggerGroup));
    }

    public async Task<bool> ContainsJobKey(string jobName, string jobGroup)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        return await scheduler.CheckExists(new JobKey(jobName, jobGroup));
    }

    public async Task<IReadOnlyCollection<string>> GetCalendarNames(
        CancellationToken cancelToken = default
    )
    {
        var scheduler = await schedulerFactory.GetScheduler(cancelToken);

        return await scheduler.GetCalendarNames(cancelToken);
    }

    public async Task PauseTrigger(string triggerName, string? triggerGroup)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.PauseTrigger(
            triggerGroup == null
                ? new TriggerKey(triggerName)
                : new TriggerKey(triggerName, triggerGroup)
        );
    }

    public async Task ResumeTrigger(string triggerName, string? triggerGroup)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.ResumeTrigger(
            triggerGroup == null
                ? new TriggerKey(triggerName)
                : new TriggerKey(triggerName, triggerGroup)
        );
    }

    public async Task<bool> DeleteSchedule(ScheduleModel model)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        if (model.JobName == null)
            return false;

        if (model.JobStatus == JobStatus.NoSchedule)
            return true;

        var jobKey = new JobKey(model.JobName, model.JobGroup);

        if (model.JobStatus == JobStatus.Error && model.TriggerName == null)
        {
            logger.LogInformation(
                "Job [{jobGroup}.{jobName}] has no trigger name. "
                    + "Cannot UncheduleJob by trigger, will delete job directly.",
                jobKey.Group,
                jobKey.Name
            );
            return await scheduler.DeleteJob(jobKey);
        }

        if (model.JobStatus == JobStatus.NoTrigger)
        {
            var triggers = await scheduler.GetTriggersOfJob(jobKey);
            if (!triggers.Any())
                return await scheduler.DeleteJob(jobKey);
            else
            {
                logger.LogWarning(
                    "Cannot delete Job [{jobGroup}.{jobName}]. There are still {triggerCount}"
                        + " trigger(s) assigned to this job.",
                    jobKey.Group,
                    jobKey.Name,
                    triggers.Count
                );
                return false;
            }
        }

        if (model.TriggerName == null)
            return false;

        var success = await scheduler.UnscheduleJob(
            model.TriggerGroup == null
                ? new TriggerKey(model.TriggerName)
                : new TriggerKey(model.TriggerName, model.TriggerGroup)
        );

        if (success)
        {
            var triggers = await scheduler.GetTriggersOfJob(jobKey);
            if (!triggers.Any())
            {
                logger.LogInformation(
                    "UnscheduleJob [{jobGroup}.{jobName}] has no more triggers. "
                        + "Determine if job was deleted.",
                    jobKey.Group,
                    jobKey.Name
                );

                if (await scheduler.CheckExists(jobKey))
                {
                    logger.LogInformation(
                        "Manually delete job [{jobGroup}.{jobName}].",
                        jobKey.Group,
                        jobKey.Name
                    );
                    return await scheduler.DeleteJob(jobKey);
                }
            }
        }

        return success;
    }

    public async Task TriggerJob(string jobName, string jobGroup)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.TriggerJob(new JobKey(jobName, jobGroup));
    }

    #region Private methods

    private async IAsyncEnumerable<ScheduleModel> GetScheduleModelsAsync(JobKey jobkey)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        IJobDetail? jobDetail = null;
        IReadOnlyCollection<ITrigger>? jobTriggers = null;
        ScheduleModel? exceptionJob = null;
        try
        {
            jobDetail = await scheduler.GetJobDetail(jobkey);
            jobTriggers = await scheduler.GetTriggersOfJob(jobkey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Cannot GetScheduleModel of job [{jobGroup}.{jobName}]",
                jobkey.Group,
                jobkey.Name
            );
            exceptionJob = new ScheduleModel
            {
                JobName = jobkey.Name,
                JobGroup = jobkey.Group,
                JobStatus = JobStatus.Error,
                ExceptionMessage = ex.Message,
            };
        }

        if (exceptionJob != null)
        {
            // job with exception
            if (jobTriggers == null || !jobTriggers.Any())
            {
                exceptionJob.TriggerType = TriggerType.Unknown;
                yield return exceptionJob;
            }
            else
            {
                foreach (var trigger in jobTriggers)
                {
                    var jobModel = await CreateScheduleModel(null, trigger);
                    jobModel.JobName = exceptionJob.JobName;
                    jobModel.JobGroup = exceptionJob.JobGroup;
                    jobModel.JobStatus = exceptionJob.JobStatus;
                    jobModel.ExceptionMessage = exceptionJob.ExceptionMessage;
                    yield return jobModel;
                }
            }
        }
        else if (jobTriggers == null || !jobTriggers.Any())
        {
            yield return new ScheduleModel
            {
                JobName = jobkey.Name,
                JobGroup = jobkey.Group,
                JobType = jobDetail?.JobType.ToString(),
                JobStatus = JobStatus.NoTrigger,
            };
        }
        else
        {
            foreach (var trigger in jobTriggers)
            {
                yield return await CreateScheduleModel(jobDetail, trigger);
            }
        }
    }

    private TriggerDetailModel CreateTriggerDetailModel(ITrigger trigger)
    {
        var triggerType = trigger.GetTriggerType();

        var model = new TriggerDetailModel
        {
            Name = trigger.Key.Name,
            Group = trigger.Key.Group,
            Description = trigger.Description,
            TriggerDataMap = trigger.JobDataMap,
            EndDate = trigger.EndTimeUtc?.Date,
            EndTimeSpan = trigger.EndTimeUtc?.TimeOfDay,
            StartDate = trigger.StartTimeUtc.Date,
            StartTimeSpan = trigger.StartTimeUtc.TimeOfDay,
            StartTimezone = TimeZoneInfo.Utc,
            TriggerType = triggerType,
            ModifiedByCalendar = trigger.CalendarName,
            Priority = trigger.Priority,
        };

        switch (trigger.MisfireInstruction)
        {
            case MisfireInstruction.IgnoreMisfirePolicy:
                model.MisfireAction = MisfireAction.IgnoreMisfirePolicy;
                break;
            // comment out same as SmartPolicy
            //case MisfireInstruction.InstructionNotSet:
            //    model.MisfireAction = MisfireAction.InstructionNotSet;
            //    break;
            case MisfireInstruction.SmartPolicy:
                model.MisfireAction = MisfireAction.SmartPolicy;
                break;
        }

        switch (triggerType)
        {
            case TriggerType.Cron:
                var cron = (ICronTrigger)trigger;
                model.CronExpression = cron.CronExpressionString;
                model.InTimeZone = cron.TimeZone;
                switch (cron.MisfireInstruction)
                {
                    case MisfireInstruction.CronTrigger.DoNothing:
                        model.MisfireAction = MisfireAction.DoNothing;
                        break;
                    case MisfireInstruction.CronTrigger.FireOnceNow:
                        model.MisfireAction = MisfireAction.FireOnceNow;
                        break;
                }
                break;
            case TriggerType.Daily:
                var daily = (IDailyTimeIntervalTrigger)trigger;
                foreach (var dow in daily.DaysOfWeek)
                {
                    model.DailyDayOfWeek[(int)dow] = true;
                }
                switch (daily.MisfireInstruction)
                {
                    case MisfireInstruction.DailyTimeIntervalTrigger.DoNothing:
                        model.MisfireAction = MisfireAction.DoNothing;
                        break;
                    case MisfireInstruction.DailyTimeIntervalTrigger.FireOnceNow:
                        model.MisfireAction = MisfireAction.FireOnceNow;
                        break;
                }
                model.RepeatCount = daily.RepeatCount;
                model.TriggerInterval = daily.RepeatInterval;
                model.TriggerIntervalUnit = daily.RepeatIntervalUnit.ToBlazingQuartzIntervalUnit();
                model.InTimeZone = daily.TimeZone;
                model.StartDailyTime = new TimeSpan(
                    daily.StartTimeOfDay.Hour,
                    daily.StartTimeOfDay.Minute,
                    daily.StartTimeOfDay.Second
                );
                model.EndDailyTime = new TimeSpan(
                    daily.EndTimeOfDay.Hour,
                    daily.EndTimeOfDay.Minute,
                    daily.EndTimeOfDay.Second
                );
                break;
            case TriggerType.Simple:
                var simple = (ISimpleTrigger)trigger;
                model = PopulateSimpleTrigger(simple, model);
                break;
            case TriggerType.Calendar:
                var calTrigger = (ICalendarIntervalTrigger)trigger;
                switch (calTrigger.MisfireInstruction)
                {
                    case MisfireInstruction.CalendarIntervalTrigger.DoNothing:
                        model.MisfireAction = MisfireAction.DoNothing;
                        break;
                    case MisfireInstruction.CalendarIntervalTrigger.FireOnceNow:
                        model.MisfireAction = MisfireAction.FireOnceNow;
                        break;
                }
                model.TriggerInterval = calTrigger.RepeatInterval;
                model.TriggerIntervalUnit =
                    calTrigger.RepeatIntervalUnit.ToBlazingQuartzIntervalUnit();
                model.InTimeZone = calTrigger.TimeZone;
                break;
        }

        return model;
    }

    private IJobDetail CreateJobDetail(JobDetailModel jobDetailModel)
    {
        ArgumentNullException.ThrowIfNull(jobDetailModel.JobClass);

        return JobBuilder
            .Create(jobDetailModel.JobClass)
            .WithIdentity(jobDetailModel.Name, jobDetailModel.Group)
            .WithDescription(jobDetailModel.Description)
            .UsingJobData(new JobDataMap(jobDetailModel.JobDataMap))
            .StoreDurably(jobDetailModel.IsDurable)
            .Build();
    }

    private ITrigger BuildTrigger(TriggerDetailModel triggerDetailModel, JobKey? jobKey = null)
    {
        var tbldr = TriggerBuilder
            .Create()
            .WithIdentity(triggerDetailModel.Name, triggerDetailModel.Group)
            .WithDescription(triggerDetailModel.Description)
            .WithPriority(triggerDetailModel.Priority)
            .UsingJobData(new JobDataMap(triggerDetailModel.TriggerDataMap))
            .ModifiedByCalendar(triggerDetailModel.ModifiedByCalendar);

        if (jobKey != null)
        {
            tbldr.ForJob(jobKey);
        }

        var startTime = triggerDetailModel.StartDateTimeUtc;
        if (startTime.HasValue)
        {
            tbldr = tbldr.StartAt(startTime.Value);
        }
        else
        {
            tbldr = tbldr.StartNow();
        }

        tbldr.EndAt(triggerDetailModel.EndDateTimeUtc);

        switch (triggerDetailModel.TriggerType)
        {
            case TriggerType.Cron:
                ArgumentNullException.ThrowIfNull(triggerDetailModel.CronExpression);
                tbldr = tbldr.WithCronSchedule(
                    triggerDetailModel.CronExpression,
                    x =>
                    {
                        switch (triggerDetailModel.MisfireAction)
                        {
                            case MisfireAction.DoNothing:
                                x.WithMisfireHandlingInstructionDoNothing();
                                break;
                            case MisfireAction.FireOnceNow:
                                x.WithMisfireHandlingInstructionFireAndProceed();
                                break;
                            case MisfireAction.IgnoreMisfirePolicy:
                                x.WithMisfireHandlingInstructionIgnoreMisfires();
                                break;
                        }
                        x.InTimeZone(triggerDetailModel.InTimeZone);
                    }
                );
                break;
            case TriggerType.Daily:
                tbldr = tbldr.WithDailyTimeIntervalSchedule(x =>
                {
                    switch (triggerDetailModel.MisfireAction)
                    {
                        case MisfireAction.DoNothing:
                            x.WithMisfireHandlingInstructionDoNothing();
                            break;
                        case MisfireAction.FireOnceNow:
                            x.WithMisfireHandlingInstructionFireAndProceed();
                            break;
                        case MisfireAction.IgnoreMisfirePolicy:
                            x.WithMisfireHandlingInstructionIgnoreMisfires();
                            break;
                    }
                    x.OnDaysOfTheWeek(triggerDetailModel.GetDailyOnDaysOfWeek());
                    if (triggerDetailModel.StartDailyTime.HasValue)
                    {
                        x.StartingDailyAt(triggerDetailModel.StartDailyTime.Value.ToTimeOfDay());
                    }
                    if (triggerDetailModel.EndDailyTime.HasValue)
                    {
                        x.EndingDailyAt(triggerDetailModel.EndDailyTime.Value.ToTimeOfDay());
                    }
                    x.InTimeZone(triggerDetailModel.InTimeZone);
                    if (
                        triggerDetailModel.TriggerInterval > 0
                        && triggerDetailModel.TriggerIntervalUnit.HasValue
                    )
                    {
                        x.WithInterval(
                            triggerDetailModel.TriggerInterval,
                            triggerDetailModel.TriggerIntervalUnit.Value.ToQuartzIntervalUnit()
                        );
                    }
                    if (triggerDetailModel.RepeatCount > 0)
                        x.WithRepeatCount(triggerDetailModel.RepeatCount);
                });
                break;
            case TriggerType.Simple:
                tbldr = tbldr.WithSimpleSchedule(x =>
                {
                    switch (triggerDetailModel.MisfireAction)
                    {
                        case MisfireAction.FireNow:
                            x.WithMisfireHandlingInstructionFireNow();
                            break;
                        case MisfireAction.RescheduleNextWithExistingCount:
                            x.WithMisfireHandlingInstructionNextWithExistingCount();
                            break;
                        case MisfireAction.RescheduleNextWithRemainingCount:
                            x.WithMisfireHandlingInstructionNextWithRemainingCount();
                            break;
                        case MisfireAction.RescheduleNowWithExistingRepeatCount:
                            x.WithMisfireHandlingInstructionNowWithExistingCount();
                            break;
                        case MisfireAction.RescheduleNowWithRemainingRepeatCount:
                            x.WithMisfireHandlingInstructionNowWithRemainingCount();
                            break;
                        case MisfireAction.IgnoreMisfirePolicy:
                            x.WithMisfireHandlingInstructionIgnoreMisfires();
                            break;
                    }

                    if (
                        triggerDetailModel.TriggerInterval > 0
                        && triggerDetailModel.TriggerIntervalUnit.HasValue
                    )
                    {
                        TimeSpan timeSpan;
                        switch (triggerDetailModel.TriggerIntervalUnit.Value)
                        {
                            case IntervalUnit.Millisecond:
                                timeSpan = TimeSpan.FromMilliseconds(
                                    triggerDetailModel.TriggerInterval
                                );
                                break;
                            case IntervalUnit.Second:
                                timeSpan = TimeSpan.FromSeconds(triggerDetailModel.TriggerInterval);
                                break;
                            case IntervalUnit.Minute:
                                timeSpan = TimeSpan.FromMinutes(triggerDetailModel.TriggerInterval);
                                break;
                            case IntervalUnit.Hour:
                                timeSpan = TimeSpan.FromHours(triggerDetailModel.TriggerInterval);
                                break;
                            case IntervalUnit.Day:
                                timeSpan = TimeSpan.FromDays(triggerDetailModel.TriggerInterval);
                                break;
                            default:
                                throw new NotSupportedException(
                                    $"Interval unit {triggerDetailModel.TriggerIntervalUnit} is not supported for SimpleTrigger."
                                );
                        }
                        x.WithInterval(timeSpan);
                    }

                    if (triggerDetailModel.RepeatForever)
                        x.RepeatForever();
                    else
                        x.WithRepeatCount(triggerDetailModel.RepeatCount);
                });
                break;
            case TriggerType.Calendar:
                tbldr = tbldr.WithCalendarIntervalSchedule(x =>
                {
                    switch (triggerDetailModel.MisfireAction)
                    {
                        case MisfireAction.DoNothing:
                            x.WithMisfireHandlingInstructionDoNothing();
                            break;
                        case MisfireAction.FireOnceNow:
                            x.WithMisfireHandlingInstructionFireAndProceed();
                            break;
                        case MisfireAction.IgnoreMisfirePolicy:
                            x.WithMisfireHandlingInstructionIgnoreMisfires();
                            break;
                    }

                    x.InTimeZone(triggerDetailModel.InTimeZone);
                    if (
                        triggerDetailModel.TriggerInterval > 0
                        && triggerDetailModel.TriggerIntervalUnit.HasValue
                    )
                    {
                        x.WithInterval(
                            triggerDetailModel.TriggerInterval,
                            triggerDetailModel.TriggerIntervalUnit.Value.ToQuartzIntervalUnit()
                        );
                    }
                });
                break;
        }

        return tbldr.Build();
    }

    private TriggerDetailModel PopulateSimpleTrigger(
        ISimpleTrigger simple,
        TriggerDetailModel model
    )
    {
        switch (simple.MisfireInstruction)
        {
            case MisfireInstruction.SimpleTrigger.RescheduleNextWithExistingCount:
                model.MisfireAction = MisfireAction.RescheduleNextWithExistingCount;
                break;
            case MisfireInstruction.SimpleTrigger.RescheduleNextWithRemainingCount:
                model.MisfireAction = MisfireAction.RescheduleNextWithRemainingCount;
                break;
            case MisfireInstruction.SimpleTrigger.RescheduleNowWithExistingRepeatCount:
                model.MisfireAction = MisfireAction.RescheduleNowWithExistingRepeatCount;
                break;
            case MisfireInstruction.SimpleTrigger.RescheduleNowWithRemainingRepeatCount:
                model.MisfireAction = MisfireAction.RescheduleNowWithRemainingRepeatCount;
                break;
            case MisfireInstruction.SimpleTrigger.FireNow:
                model.MisfireAction = MisfireAction.FireNow;
                break;
        }
        if (simple.RepeatCount >= 0)
            model.RepeatCount = simple.RepeatCount;
        else
            model.RepeatForever = true;

        var total = simple.RepeatInterval.TotalHours;
        if (Math.Round(total) == total)
        {
            model.TriggerInterval = Convert.ToInt32(total);
            model.TriggerIntervalUnit = IntervalUnit.Hour;
        }
        else
        {
            total = simple.RepeatInterval.TotalMinutes;
            if (Math.Round(total) == total)
            {
                model.TriggerInterval = Convert.ToInt32(total);
                model.TriggerIntervalUnit = IntervalUnit.Minute;
            }
            else
            {
                total = simple.RepeatInterval.TotalSeconds;
                if (Math.Round(total) == total)
                {
                    model.TriggerInterval = Convert.ToInt32(total);
                    model.TriggerIntervalUnit = IntervalUnit.Second;
                }
                //else
                //{
                //    total = simple.RepeatInterval.TotalMilliseconds;
                //    if (Math.Round(total) == total)
                //    {
                //        model.TriggerInterval = Convert.ToInt32(total);
                //        model.TriggerIntervalUnit = IntervalUnit.Millisecond;
                //    }
                //}
            }
        }

        return model;
    }

    public async Task PauseAllSchedules()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.PauseAll();
    }

    public async Task ResumeAllSchedules()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.ResumeAll();
    }

    public async Task ShutdownScheduler()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.Shutdown();
    }

    public async Task StartScheduler()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.Start();
    }

    public async Task StandbyScheduler()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.Standby();
    }

    #endregion Private methods
}
