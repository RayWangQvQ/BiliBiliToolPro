using System;
using System.Globalization;
using System.Threading.Channels;
using BlazingQuartz.Core.Jobs;
using BlazingQuartz.Core.Services;
using BlazingQuartz.Jobs.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Domain;

namespace BlazingQuartz.Core.History;

internal class SchedulerEventLoggingService : BackgroundService, ISchedulerEventLoggingService
{
    private const int RESULT_MAX_LENGTH = 8000;
    private const int MAX_BATCH_SIZE = 50;

    private readonly IServiceProvider _svcProvider;
    private readonly ISchedulerListenerService _schLisSvc;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<SchedulerEventLoggingService> _logger;
    private readonly Channel<Func<IExecutionLogStore, CancellationToken, ValueTask>> _taskQueue;
    private readonly BlazingQuartzCoreOptions _options;

    public SchedulerEventLoggingService(
        ILogger<SchedulerEventLoggingService> logger,
        IServiceProvider serviceProvider,
        ISchedulerListenerService listenerSvc,
        ISchedulerFactory schFactory,
        IOptions<BlazingQuartzCoreOptions> options
    )
    {
        _logger = logger;
        _svcProvider = serviceProvider;
        _schLisSvc = listenerSvc;
        _schedulerFactory = schFactory;
        _options = options.Value;
        _taskQueue = Channel.CreateUnbounded<
            Func<IExecutionLogStore, CancellationToken, ValueTask>
        >(new UnboundedChannelOptions { SingleReader = true });

        Init();
    }

    public override void Dispose()
    {
        _schLisSvc.OnJobToBeExecuted -= _schLisSvc_OnJobToBeExecuted;
        _schLisSvc.OnJobWasExecuted -= _schLisSvc_OnJobWasExecuted;
        _schLisSvc.OnJobExecutionVetoed -= _schLisSvc_OnJobExecutionVetoed;
        _schLisSvc.OnJobDeleted -= _schLisSvc_OnJobDeleted;
        _schLisSvc.OnJobInterrupted -= _schLisSvc_OnJobInterrupted;
        _schLisSvc.OnSchedulerError -= _schLisSvc_OnSchedulerError;
        _schLisSvc.OnTriggerMisfired -= _schLisSvc_OnTriggerMisfired;
        _schLisSvc.OnTriggerPaused -= _schLisSvc_OnTriggerPaused;
        _schLisSvc.OnTriggerResumed -= _schLisSvc_OnTriggerResumed;
        _schLisSvc.OnTriggerFinalized -= _schLisSvc_OnTriggerFinalized;
        _schLisSvc.OnJobScheduled -= _schLisSvc_OnJobScheduled;
        base.Dispose();
    }

    void Init()
    {
        _schLisSvc.OnJobToBeExecuted += _schLisSvc_OnJobToBeExecuted;
        _schLisSvc.OnJobWasExecuted += _schLisSvc_OnJobWasExecuted;
        _schLisSvc.OnJobExecutionVetoed += _schLisSvc_OnJobExecutionVetoed;
        _schLisSvc.OnJobDeleted += _schLisSvc_OnJobDeleted;
        _schLisSvc.OnJobInterrupted += _schLisSvc_OnJobInterrupted;
        _schLisSvc.OnSchedulerError += _schLisSvc_OnSchedulerError;
        _schLisSvc.OnTriggerMisfired += _schLisSvc_OnTriggerMisfired;
        _schLisSvc.OnTriggerPaused += _schLisSvc_OnTriggerPaused;
        _schLisSvc.OnTriggerResumed += _schLisSvc_OnTriggerResumed;
        _schLisSvc.OnTriggerFinalized += _schLisSvc_OnTriggerFinalized;
        _schLisSvc.OnJobScheduled += _schLisSvc_OnJobScheduled;
    }

    private void _schLisSvc_OnJobScheduled(object? sender, Events.EventArgs<ITrigger> e)
    {
        var jKey = e.Args.JobKey;
        var tKey = e.Args.Key;
        var log = new ExecutionLog
        {
            JobName = jKey.Name,
            JobGroup = jKey.Group,
            TriggerName = tKey.Name,
            TriggerGroup = tKey.Group,
            LogType = LogType.Trigger,
            Result = "Job scheduled",
        };
        QueueInsertTask(log);
    }

    private void _schLisSvc_OnTriggerFinalized(object? sender, Events.EventArgs<ITrigger> e)
    {
        var jKey = e.Args.JobKey;
        var tKey = e.Args.Key;
        var log = new ExecutionLog
        {
            JobName = jKey.Name,
            JobGroup = jKey.Group,
            TriggerName = tKey.Name,
            TriggerGroup = tKey.Group,
            LogType = LogType.Trigger,
            Result = "Trigger ended",
        };
        QueueInsertTask(log);
    }

    private void _schLisSvc_OnTriggerResumed(object? sender, Events.EventArgs<TriggerKey> e)
    {
        var tKey = e.Args;
        var log = new ExecutionLog
        {
            TriggerName = tKey.Name,
            TriggerGroup = tKey.Group,
            LogType = LogType.Trigger,
            Result = "Trigger resumed",
        };
        QueueGetJobKeyAndInsertTask(log);
    }

    private void _schLisSvc_OnTriggerPaused(object? sender, Events.EventArgs<TriggerKey> e)
    {
        var tKey = e.Args;
        var log = new ExecutionLog
        {
            TriggerName = tKey.Name,
            TriggerGroup = tKey.Group,
            LogType = LogType.Trigger,
            Result = "Trigger paused",
        };
        QueueGetJobKeyAndInsertTask(log);
    }

    private void _schLisSvc_OnTriggerMisfired(object? sender, Events.EventArgs<ITrigger> e)
    {
        var jKey = e.Args.JobKey;
        var tKey = e.Args.Key;
        var log = new ExecutionLog
        {
            LogType = LogType.Trigger,
            JobName = jKey.Name,
            JobGroup = jKey.Group,
            TriggerName = tKey.Name,
            TriggerGroup = tKey.Group,
            Result = "Trigger misfired",
        };
        QueueInsertTask(log);
    }

    private void _schLisSvc_OnSchedulerError(object? sender, Events.SchedulerErrorEventArgs e)
    {
        var log = new ExecutionLog
        {
            LogType = LogType.System,
            IsException = true,
            ErrorMessage = e.ErrorMessage,
            ExecutionLogDetail = new() { ErrorStackTrace = e.Exception.NonNullStackTrace() },
        };
        QueueInsertTask(log);
    }

    private void _schLisSvc_OnJobInterrupted(object? sender, Events.EventArgs<JobKey> e)
    {
        var jKey = e.Args;
        var log = new ExecutionLog
        {
            JobName = jKey.Name,
            JobGroup = jKey.Group,
            LogType = LogType.System,
            Result = "Job interrupted",
        };
        QueueInsertTask(log);
    }

    private void _schLisSvc_OnJobDeleted(object? sender, Events.EventArgs<JobKey> e)
    {
        JobKey jKey = e.Args;
        var log = new ExecutionLog
        {
            JobName = jKey.Name,
            JobGroup = jKey.Group,
            LogType = LogType.System,
            Result = "Job deleted",
        };
        QueueInsertTask(log);
    }

    internal void _schLisSvc_OnJobExecutionVetoed(
        object? sender,
        Events.EventArgs<Quartz.IJobExecutionContext> e
    )
    {
        var log = CreateScheduleJobLogEntry(e.Args, defaultIsSuccess: false);
        log.IsVetoed = true;
        QueueUpdateTask(log);
    }

    internal void _schLisSvc_OnJobWasExecuted(object? sender, Events.JobWasExecutedEventArgs e)
    {
        QueueUpdateTask(CreateScheduleJobLogEntry(e.JobExecutionContext, e.JobException, true));
    }

    internal void _schLisSvc_OnJobToBeExecuted(
        object? sender,
        Events.EventArgs<Quartz.IJobExecutionContext> e
    )
    {
        QueueInsertTask(CreateScheduleJobLogEntry(e.Args));
    }

    private async Task AddHousekeepingSchedule(IScheduler scheduler)
    {
        var housekeepingJobName = "Housekeep ExecutionLogs (bili)";
        var triggerKey = new TriggerKey(housekeepingJobName, Constants.SYSTEM_GROUP);

        if (!string.IsNullOrEmpty(_options.HousekeepingCronSchedule))
        {
            var reschedule = true;

            // determine if already exists
            if (await scheduler.CheckExists(triggerKey))
            {
                // determine if same cron schedule
                var trig = await scheduler.GetTrigger(triggerKey);
                if (trig != null && trig.GetTriggerType() == TriggerType.Cron)
                {
                    var cronTrigger = (ICronTrigger)trig;
                    if (cronTrigger.CronExpressionString == _options.HousekeepingCronSchedule)
                        reschedule = false;
                }
            }

            if (reschedule)
            {
                // create new one
                IJobDetail job = JobBuilder
                    .Create<HousekeepExecutionLogsJob>()
                    .WithIdentity(housekeepingJobName, Constants.SYSTEM_GROUP)
                    .Build();

                ITrigger trigger = TriggerBuilder
                    .Create()
                    .WithIdentity(housekeepingJobName, Constants.SYSTEM_GROUP)
                    .StartNow()
                    .WithCronSchedule(_options.HousekeepingCronSchedule)
                    .Build();

                ITrigger nowTrigger = TriggerBuilder
                    .Create()
                    .WithIdentity("Housekeep ExecutionLogs now (bili)", Constants.SYSTEM_GROUP)
                    .StartNow()
                    .Build();

                await scheduler.ScheduleJob(job, new[] { trigger, nowTrigger }, true);
            }
        }
        else
        {
            // delete housekeeping schedule
            if (await scheduler.CheckExists(triggerKey))
            {
                _logger.LogInformation(
                    "Housekeeping ExecutionLogs has no cron schedule specified. Delete scheduled job"
                );
                await scheduler.UnscheduleJob(triggerKey);
            }
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var scheduler = await _schedulerFactory.GetScheduler();

        await AddHousekeepingSchedule(scheduler);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await MarkIncompleteExecution(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessTaskAsync(stoppingToken);
        }
    }

    internal async Task MarkIncompleteExecution(CancellationToken stoppingToken)
    {
        try
        {
            using (IServiceScope scope = _svcProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IExecutionLogStore>();

                await repo.MarkExecutingJobAsIncomplete();
            }
        }
        catch (OperationCanceledException)
        {
            // Prevent throwing if stoppingToken was signaled
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while updating executing status to incomplete status."
            );
        }
    }

    internal async Task ProcessTaskAsync(CancellationToken stoppingToken = default)
    {
        var batch = await GetBatch(stoppingToken);

        _logger.LogInformation(
            "Got a batch with {taskCount} task(s). Saving to data store.",
            batch.Count
        );

        try
        {
            using (IServiceScope scope = _svcProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IExecutionLogStore>();

                foreach (var workItem in batch)
                {
                    await workItem(repo, stoppingToken);
                }

                try
                {
                    await repo.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while saving execution logs.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Prevent throwing if stoppingToken was signaled
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred executing task work item.");
        }
    }

    private ExecutionLog CreateScheduleJobLogEntry(
        IJobExecutionContext context,
        JobExecutionException? jobException = null,
        bool? defaultIsSuccess = null
    )
    {
        var log = new ExecutionLog
        {
            RunInstanceId = context.FireInstanceId,
            JobGroup = context.JobDetail.Key.Group,
            JobName = context.JobDetail.Key.Name,
            TriggerName = context.Trigger.Key.Name,
            TriggerGroup = context.Trigger.Key.Group,
            FireTimeUtc = context.FireTimeUtc,
            ScheduleFireTimeUtc = context.ScheduledFireTimeUtc,
            RetryCount = context.RefireCount,
            JobRunTime = context.JobRunTime,
            LogType = LogType.ScheduleJob,
        };
        var logDetail = new ExecutionLogDetail();

        log.ReturnCode = context.GetReturnCode();
        log.IsSuccess = context.GetIsSuccess();

        if (log.IsSuccess is null)
            log.IsSuccess = defaultIsSuccess;

        var execDetail = context.GetExecutionDetails();
        if (!string.IsNullOrEmpty(execDetail))
        {
            logDetail.ExecutionDetails = execDetail;
            log.ExecutionLogDetail = logDetail;
        }

        if (jobException != null)
        {
            log.ErrorMessage = jobException.Message;
            log.ExecutionLogDetail = logDetail;
            logDetail.ErrorCode = jobException.HResult;
            logDetail.ErrorStackTrace = jobException.ToString();
            logDetail.ErrorHelpLink = jobException.HelpLink;

            if (log.ReturnCode == null)
                log.ReturnCode = jobException.HResult.ToString();

            log.IsException = true;
            log.IsSuccess = false;
        }
        else
        {
            if (context.Result != null)
            {
                var result = Convert.ToString(context.Result, CultureInfo.InvariantCulture);
                log.Result = result?.Substring(0, Math.Min(result.Length, RESULT_MAX_LENGTH));
            }
        }

        return log;
    }

    private async Task<List<Func<IExecutionLogStore, CancellationToken, ValueTask>>> GetBatch(
        CancellationToken cancellationToken
    )
    {
        await _taskQueue.Reader.WaitToReadAsync(cancellationToken);

        var batch = new List<Func<IExecutionLogStore, CancellationToken, ValueTask>>();

        while (batch.Count < MAX_BATCH_SIZE && _taskQueue.Reader.TryRead(out var dbTask))
        {
            batch.Add(dbTask);
        }

        return batch;
    }

    void QueueUpdateTask(ExecutionLog log)
    {
        QueueTask(
            async (IExecutionLogStore repo, CancellationToken cancelToken) =>
            {
                try
                {
                    if (!repo.Exists(log))
                        await repo.SaveChangesAsync(cancelToken);

                    await repo.UpdateExecutionLog(log);
                }
                catch (Exception ex)
                {
                    if (log.LogType == LogType.ScheduleJob)
                    {
                        _logger.LogError(
                            ex,
                            "Error occurred while updating execution log with job key [{jobGroup}.{jobName}] "
                                + "run instance id [{runInstanceId}].",
                            log.JobGroup,
                            log.JobName,
                            log.RunInstanceId
                        );
                    }
                    else
                    {
                        _logger.LogError(
                            ex,
                            "Error occurred while updating {logType} execution log with "
                                + "job key [{jobGroup}.{jobName}] trigger key [{triggerGroup}.{triggerName}].",
                            log.LogType,
                            log.JobGroup,
                            log.JobName,
                            log.TriggerGroup,
                            log.TriggerName
                        );
                    }
                }
            }
        );
    }

    void QueueGetJobKeyAndInsertTask(ExecutionLog log)
    {
        QueueTask(
            async (IExecutionLogStore repo, CancellationToken cancelToken) =>
            {
                try
                {
                    if (log.JobName == null && log.TriggerName != null && log.TriggerGroup != null)
                    {
                        // when there is no job name but has trigger name
                        // try to determine the job name
                        var scheduler = await _schedulerFactory.GetScheduler();
                        var trigger = await scheduler.GetTrigger(
                            new TriggerKey(log.TriggerName, log.TriggerGroup),
                            cancelToken
                        );
                        if (trigger != null)
                        {
                            log.JobName = trigger.JobKey.Name;
                            log.JobGroup = trigger.JobKey.Group;
                        }
                    }

                    await repo.AddExecutionLog(log, cancelToken);
                }
                catch (Exception ex)
                {
                    if (log.LogType == LogType.ScheduleJob)
                    {
                        _logger.LogError(
                            ex,
                            "Error occurred while adding execution log with job key [{jobGroup}.{jobName}] "
                                + "run instance id [{runInstanceId}].",
                            log.JobGroup,
                            log.JobName,
                            log.RunInstanceId
                        );
                    }
                    else
                    {
                        _logger.LogError(
                            ex,
                            "Error occurred while adding {logType} execution log with "
                                + "job key [{jobGroup}.{jobName}] trigger key [{triggerGroup}.{triggerName}].",
                            log.LogType,
                            log.JobGroup,
                            log.JobName,
                            log.TriggerGroup,
                            log.TriggerName
                        );
                    }
                }
            }
        );
    }

    void QueueInsertTask(ExecutionLog log)
    {
        QueueTask(
            async (IExecutionLogStore repo, CancellationToken cancelToken) =>
            {
                try
                {
                    await repo.AddExecutionLog(log, cancelToken);
                }
                catch (Exception ex)
                {
                    if (log.LogType == LogType.ScheduleJob)
                    {
                        _logger.LogError(
                            ex,
                            "Error occurred while adding execution log with job key [{jobGroup}.{jobName}] "
                                + "run instance id [{runInstanceId}].",
                            log.JobGroup,
                            log.JobName,
                            log.RunInstanceId
                        );
                    }
                    else
                    {
                        _logger.LogError(
                            ex,
                            "Error occurred while adding {logType} execution log with "
                                + "job key [{jobGroup}.{jobName}] trigger key [{triggerGroup}.{triggerName}].",
                            log.LogType,
                            log.JobGroup,
                            log.JobName,
                            log.TriggerGroup,
                            log.TriggerName
                        );
                    }
                }
            }
        );
    }

    void QueueTask(Func<IExecutionLogStore, CancellationToken, ValueTask> task)
    {
        if (!_taskQueue.Writer.TryWrite(task))
        {
            // Should not happen since it's unbounded Channel. It 'should' only fail if we call writer.Complete()
            throw new InvalidOperationException("Failed to write the log message");
        }
    }
}
