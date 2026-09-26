using System.Data.Common;
using BlazingQuartz.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Ray.BiliBiliTool.Host.IntegrationTests.Support;
using Ray.BiliBiliTool.Infrastructure.EF;

namespace Ray.BiliBiliTool.Host.IntegrationTests;

/// <summary>
/// Runs against a booted host with the real SQLite persistent store, so every assertion below
/// passes through the Quartz 4 job store rather than an in-memory fake.
/// </summary>
[Collection("Host boot")]
public class QuartzRuntimeIntegrationTests
{
    private const string Group = "QuartzRuntimeTests";

    /// <summary>Jan 1 at midnight, so nothing fires while the test runs.</summary>
    private const string DormantCron = "0 0 0 1 1 ?";

    private sealed class NoOpJob : IJob
    {
        public ValueTask Execute(
            IJobExecutionContext context,
            CancellationToken cancellationToken
        ) => ValueTask.CompletedTask;
    }

    private static async Task<IScheduler> GetSchedulerAsync(WebHostFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var schedulerFactory = scope.ServiceProvider.GetRequiredService<ISchedulerFactory>();
        return await schedulerFactory.GetScheduler();
    }

    [Fact]
    public async Task Cron_trigger_scheduled_through_the_real_store_computes_a_next_fire_time()
    {
        using var factory = new WebHostFactory();
        var scheduler = await GetSchedulerAsync(factory);
        var key = new JobKey("cron-next-fire", Group);
        var triggerKey = new TriggerKey("cron-next-fire.trigger", Group);

        try
        {
            await scheduler.ScheduleJob(
                JobBuilder.Create<NoOpJob>().WithIdentity(key).StoreDurably().Build(),
                TriggerBuilder
                    .Create()
                    .ForJob(key)
                    .WithIdentity(triggerKey)
                    .WithCronSchedule(DormantCron)
                    .Build()
            );

            var trigger = await scheduler.GetTrigger(triggerKey);

            trigger.Should().NotBeNull();
            trigger!.NextFireTimeUtc.Should().NotBeNull("cron parsing must work under Quartz 4");
            trigger.NextFireTimeUtc!.Value.Should().BeAfter(scheduler.TimeProvider.GetUtcNow());
            ((ICronTrigger)trigger).CronExpressionString.Should().Be(DormantCron);
        }
        finally
        {
            await scheduler.DeleteJob(key);
        }
    }

    [Fact]
    public async Task Pausing_a_job_group_writes_a_QRTZ_PAUSED_JOB_GRPS_row_and_resuming_removes_it()
    {
        using var factory = new WebHostFactory();
        var scheduler = await GetSchedulerAsync(factory);
        var key = new JobKey("paused-group", Group);
        var triggerKey = new TriggerKey("paused-group.trigger", Group);

        await scheduler.ScheduleJob(
            JobBuilder.Create<NoOpJob>().WithIdentity(key).StoreDurably().Build(),
            TriggerBuilder
                .Create()
                .ForJob(key)
                .WithIdentity(triggerKey)
                .WithCronSchedule(DormantCron)
                .Build()
        );

        try
        {
            await scheduler.PauseJobGroups(GroupMatcher<JobKey>.GroupEquals(Group));
            (await CountPausedGroupsAsync(factory)).Should().Be(1);
            (await scheduler.GetTriggerState(triggerKey)).Should().Be(TriggerState.Paused);
        }
        finally
        {
            // Resume here: a leaked paused group would silently affect the next test in the store.
            await scheduler.ResumeJobGroups(GroupMatcher<JobKey>.GroupEquals(Group));
            await scheduler.DeleteJob(key);
        }

        (await CountPausedGroupsAsync(factory)).Should().Be(0);
    }

    /// <summary>
    /// The expected numbers are what Quartz 3.22 wrote into old users' QRTZ_TRIGGERS rows. 4.1.1
    /// renamed the vocabulary but must keep producing the same code, or a schedule saved before
    /// the upgrade silently changes its misfire policy.
    /// </summary>
    [Fact]
    public async Task Misfire_policies_round_trip_through_the_store_with_the_Quartz_3_22_codes()
    {
        using var factory = new WebHostFactory();
        var scheduler = await GetSchedulerAsync(factory);
        var start = DateTimeOffset.UtcNow.AddYears(1);

        var cases = new (string Name, Action<TriggerBuilder<IJob>> Build, int ExpectedCode)[]
        {
            (
                "cron-do-nothing",
                t =>
                    t.WithCronSchedule(
                        DormantCron,
                        x => x.WithMisfireInstruction(CronTriggerMisfireInstruction.DoNothing)
                    ),
                2
            ),
            (
                "cron-fire-once-now",
                t =>
                    t.WithCronSchedule(
                        DormantCron,
                        x => x.WithMisfireInstruction(CronTriggerMisfireInstruction.FireAndProceed)
                    ),
                1
            ),
            (
                "cron-ignore-misfires",
                t =>
                    t.WithCronSchedule(
                        DormantCron,
                        x => x.WithMisfireInstruction(CronTriggerMisfireInstruction.IgnoreMisfires)
                    ),
                -1
            ),
            (
                "simple-reschedule-next-with-existing-count",
                t =>
                    t.WithSimpleSchedule(x =>
                        x.WithInterval(TimeSpan.FromHours(1))
                            .WithMisfireInstruction(
                                SimpleTriggerMisfireInstruction.NextWithExistingCount
                            )
                    ),
                5
            ),
            (
                "simple-reschedule-now-with-remaining-count",
                t =>
                    t.WithSimpleSchedule(x =>
                        x.WithInterval(TimeSpan.FromHours(1))
                            .WithMisfireInstruction(
                                SimpleTriggerMisfireInstruction.NowWithRemainingCount
                            )
                    ),
                3
            ),
            (
                "daily-do-nothing",
                t =>
                    t.WithDailyTimeIntervalSchedule(x =>
                        x.WithInterval(1, IntervalUnit.Hour)
                            .WithMisfireInstruction(
                                DailyTimeIntervalTriggerMisfireInstruction.DoNothing
                            )
                    ),
                2
            ),
            (
                "calendar-fire-once-now",
                t =>
                    t.WithCalendarIntervalSchedule(x =>
                        x.WithInterval(1, IntervalUnit.Day)
                            .WithMisfireInstruction(
                                CalendarIntervalTriggerMisfireInstruction.FireAndProceed
                            )
                    ),
                1
            ),
        };

        foreach (var (name, build, expected) in cases)
        {
            var jobKey = new JobKey(name, Group);
            var triggerKey = new TriggerKey($"{name}.trigger", Group);

            var builder = TriggerBuilder
                .Create()
                .ForJob(jobKey)
                .WithIdentity(triggerKey)
                .StartAt(start);
            build(builder);

            await scheduler.ScheduleJob(
                JobBuilder.Create<NoOpJob>().WithIdentity(jobKey).StoreDurably().Build(),
                builder.Build()
            );

            var reloaded = await scheduler.GetTrigger(triggerKey);
            reloaded.Should().NotBeNull(name);
            reloaded!.MisfireInstructionCode.Should().Be(expected, name);

            await scheduler.DeleteJob(jobKey);
        }
    }

    /// <summary>
    /// Every ISchedulerListener callback has a default implementation in Quartz 4, so a signature the
    /// upgrade left behind still compiles and then just stops calling us - the panel goes quiet instead
    /// of failing. Dispatching through ISchedulerListener is what the scheduler does at runtime.
    /// </summary>
    [Fact]
    public async Task Scheduler_error_notifications_reach_the_events_the_panel_subscribes_to()
    {
        using var factory = new WebHostFactory();
        var scheduler = await GetSchedulerAsync(factory);
        var listener = factory.Services.GetRequiredService<ISchedulerListenerService>();
        var triggerKey = new TriggerKey("trigger-in-error", Group);
        var jobKey = new JobKey("triggers-in-error", Group);

        TriggerKey? reportedTrigger = null;
        JobKey? reportedJob = null;
        listener.OnTriggerInError += (_, e) => reportedTrigger = e.Args;
        listener.OnTriggersInError += (_, e) => reportedJob = e.Args;

        await ((ISchedulerListener)listener).TriggerInError(scheduler, triggerKey);
        await ((ISchedulerListener)listener).TriggersInError(scheduler, jobKey);

        reportedTrigger
            .Should()
            .Be(triggerKey, "TriggerInError must be the implementation Quartz dispatches to");
        reportedJob
            .Should()
            .Be(jobKey, "TriggersInError must be the implementation Quartz dispatches to");
    }

    private static async Task<int> CountPausedGroupsAsync(WebHostFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BiliDbContext>();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM QRTZ_PAUSED_JOB_GRPS WHERE JOB_GROUP = $group";
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "$group";
        parameter.Value = Group;
        command.Parameters.Add(parameter);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
