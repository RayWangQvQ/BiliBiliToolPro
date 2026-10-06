using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.DomainService;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.EF;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class DailyTaskNotificationStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 18, 0, 0, TimeSpan.FromHours(8));
    private static readonly DateOnly Day = new(2026, 10, 6);

    [Fact]
    public async Task DailySourceReadsOnlyLocalSchedulesAndSavedRecordsForEveryAccount()
    {
        var directory = Directory.CreateTempSubdirectory("daily-status-");
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Sqlite"] =
                $"Data Source={Path.Combine(directory.FullName, "test.db")}",
            ["BiliBiliCookies:0"] = "DedeUserID=1001;bili_jct=synthetic;SESSDATA=synthetic",
            ["BiliBiliCookies:1"] = "DedeUserID=1002;bili_jct=synthetic;SESSDATA=synthetic",
        };
        foreach (
            var section in new[]
            {
                "DailyTaskConfig",
                "MangaPrivilegeTaskConfig",
                "Silver2CoinTaskConfig",
                "ChargeTaskConfig",
                "VipPrivilegeConfig",
                "VipBigPointConfig",
                "LiveLotteryTaskConfig",
                "LiveFansMedalTaskConfig",
                "UnfollowBatchedTaskConfig",
            }
        )
            values[section + ":IsEnable"] = "false";
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        await using var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(config)
            .AddSingleton<CookieStrFactory<BiliCookie>>()
            .AddDbContextFactory<BiliDbContext>()
            .AddQuartz(
                new Dictionary<string, string>
                {
                    ["quartz.scheduler.instanceName"] =
                        "daily-status-" + Guid.NewGuid().ToString("N"),
                }
            )
            .BuildServiceProvider();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
        try
        {
            var key = new JobKey("MangaJob", Ray.BiliBiliTool.Web.Constants.BiliJobGroup);
            await scheduler.ScheduleJob(
                JobBuilder.Create<StandbyJob>().WithIdentity(key).Build(),
                TriggerBuilder.Create().ForJob(key).WithCronSchedule("0 0 17 * * ?").Build()
            );
            var factory = provider.GetRequiredService<IDbContextFactory<BiliDbContext>>();
            await using var db = await factory.CreateDbContextAsync();
            await db.Database.EnsureCreatedAsync();
            db.TaskRecords.AddRange(
                new TaskRecord
                {
                    UserId = 1001,
                    TaskKey = "MangaTaskAppService",
                    RecordDate = "2026-10-06",
                    Status = TaskRecordStatus.Success,
                    Trigger = TaskRecordTrigger.Scheduled,
                    CreatedAtUtc = Now.AddMinutes(-30),
                },
                new TaskRecord
                {
                    UserId = 1002,
                    TaskKey = "MangaTaskAppService",
                    RecordDate = "2026-10-06",
                    Status = TaskRecordStatus.Success,
                    Trigger = TaskRecordTrigger.Manual,
                    CreatedAtUtc = Now.AddMinutes(-30),
                }
            );
            await db.SaveChangesAsync();
            // No API or activity services are registered, and Quartz remains in standby.
            var source = new DailyTaskNotificationStatusSource(
                provider.GetRequiredService<IServiceScopeFactory>()
            );
            var status = await source.ReadAsync(Day, Now, default);
            Assert.False(status.AllFinished);
            Assert.False(status.Running);
            Assert.Equal("***1002", Assert.Single(Assert.Single(status.Items).PendingAccounts));
            Assert.Equal(2, Assert.Single(status.Items).Accounts.Count);
            Assert.Equal(
                2,
                Assert
                    .Single((await source.ReadAsync(Day, Now.AddHours(-2), default)).Items)
                    .PendingAccounts.Count
            );
            db.TaskRecords.Add(
                new TaskRecord
                {
                    UserId = 1002,
                    TaskKey = "MangaTaskAppService",
                    RecordDate = "2026-10-06",
                    Status = TaskRecordStatus.Success,
                    Trigger = TaskRecordTrigger.Scheduled,
                    CreatedAtUtc = Now,
                }
            );
            await db.SaveChangesAsync();
            Assert.True((await source.ReadAsync(Day, Now, default)).AllFinished);
            Assert.Equal(
                2,
                (await source.ReadAsync(Day.AddDays(1), Now.AddDays(1), default))
                    .Items.Single()
                    .PendingAccounts.Count
            );
            Assert.False(
                (await source.ReadAsync(Day.AddDays(1), Now.AddDays(1), default)).AllFinished
            );
        }
        finally
        {
            await scheduler.Shutdown();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            directory.Delete(true);
        }
    }

    public sealed class StandbyJob : IJob
    {
        public ValueTask Execute(IJobExecutionContext context, CancellationToken token = default) =>
            throw new InvalidOperationException("Notification checks must not execute activities");
    }

    [Fact]
    public void FutureSchedulesOtherAccountsAndLatestFailuresPreventEarlyCompletion()
    {
        TaskRecord Record(TaskRecordStatus status, DateTimeOffset when, long id = 1) =>
            new()
            {
                UserId = 1001,
                TaskKey = "DailyTaskAppService",
                Trigger = TaskRecordTrigger.Scheduled,
                Status = status,
                CreatedAtUtc = when,
                Id = id,
            };
        var records = new List<TaskRecord> { Record(TaskRecordStatus.Success, Now.AddHours(-1)) };
        Assert.True(
            DailyTaskNotificationStatusSource.ScheduledFinished(
                records,
                1001,
                "DailyTaskAppService",
                Now.AddHours(-2),
                Now
            )
        );
        Assert.False(
            DailyTaskNotificationStatusSource.ScheduledFinished(
                records,
                1001,
                "DailyTaskAppService",
                Now.AddHours(1),
                Now
            )
        );
        Assert.False(
            DailyTaskNotificationStatusSource.ScheduledFinished(
                records,
                1002,
                "DailyTaskAppService",
                Now.AddHours(-2),
                Now
            )
        );
        records.Add(Record(TaskRecordStatus.Failed, Now, 2));
        Assert.False(
            DailyTaskNotificationStatusSource.ScheduledFinished(
                records,
                1001,
                "DailyTaskAppService",
                Now.AddHours(-2),
                Now
            )
        );
    }

    [Theory]
    [InlineData(TaskRecordTrigger.Manual)]
    [InlineData(TaskRecordTrigger.Auto)]
    public void RecoveryRecordsCannotFinishScheduledTask(TaskRecordTrigger trigger)
    {
        var records = new[]
        {
            new TaskRecord
            {
                UserId = 1001,
                TaskKey = "DailyTaskAppService",
                Trigger = trigger,
                Status = TaskRecordStatus.Success,
                CreatedAtUtc = Now,
            },
        };
        Assert.False(
            DailyTaskNotificationStatusSource.ScheduledFinished(
                records,
                1001,
                "DailyTaskAppService",
                Now.AddHours(-2),
                Now
            )
        );
        records[0].Trigger = TaskRecordTrigger.Scheduled;
        records[0].TaskItemKey = "Login";
        Assert.False(
            DailyTaskNotificationStatusSource.ScheduledFinished(
                records,
                1001,
                "DailyTaskAppService",
                Now.AddHours(-2),
                Now
            )
        );
    }

    [Fact]
    public void WaitingForLiveMedalsBlocksEarlySummaryUntilDoneOrDailyBudgetReached()
    {
        var options = new LiveFansMedalTaskOptions
        {
            EnableDanmaku = false,
            EnableWatch = false,
            DailyLikeNumber = 300,
        };
        var gate = new LiveFansMedalExecutionGate(new Clock());
        var card = new LiveMedalCard(
            1,
            "示例主播",
            "示例牌",
            10,
            false,
            true,
            false,
            [new("like", "点赞300次", "0/300", false, 0)],
            null
        );
        var snapshot = new LiveMedalSnapshot([card], Now);
        Assert.False(
            DailyTaskNotificationStatusSource.MedalFinished(snapshot, options, 1001, Day, Now, gate)
        );
        Assert.True(
            DailyTaskNotificationStatusSource.MedalFinished(
                snapshot with
                {
                    Medals =
                    [
                        card with
                        {
                            Tasks = [new("like", "点赞300次", "300/300", true, 100)],
                        },
                    ],
                },
                options,
                1001,
                Day,
                Now,
                gate
            )
        );
        Assert.Equal(300, gate.Reserve("1001", 1, "like", 300, 300));
        Assert.True(
            DailyTaskNotificationStatusSource.MedalFinished(snapshot, options, 1001, Day, Now, gate)
        );
        Assert.False(
            DailyTaskNotificationStatusSource.MedalFinished(snapshot, options, 1002, Day, Now, gate)
        );
    }

    [Fact]
    public void MissingStaleOrFailedMedalProgressRemainsPending()
    {
        var options = new LiveFansMedalTaskOptions();
        var gate = new LiveFansMedalExecutionGate(new Clock());
        Assert.False(
            DailyTaskNotificationStatusSource.MedalFinished(null, options, 1001, Day, Now, gate)
        );
        Assert.False(
            DailyTaskNotificationStatusSource.MedalFinished(
                new([], Now.AddMinutes(-40)),
                options,
                1001,
                Day,
                Now,
                gate
            )
        );
        Assert.False(
            DailyTaskNotificationStatusSource.MedalFinished(
                new([], Now, "暂未获取"),
                options,
                1001,
                Day,
                Now,
                gate
            )
        );
        Assert.False(
            DailyTaskNotificationStatusSource.MedalFinished(
                new([], Now.AddDays(-1)),
                options,
                1001,
                Day,
                Now,
                gate
            )
        );
        var oldCard = new LiveMedalCard(
            1,
            "示例主播",
            "示例牌",
            10,
            false,
            true,
            true,
            [],
            null,
            ProgressUpdatedAt: Now.AddDays(-1)
        );
        Assert.False(
            DailyTaskNotificationStatusSource.MedalFinished(
                new([oldCard], Now),
                options,
                1001,
                Day,
                Now,
                gate
            )
        );
    }

    [Fact]
    public void ExcludedMedalsAndDisabledActionsDoNotBlockSummary()
    {
        var options = new LiveFansMedalTaskOptions { ExcludedAnchorIds = "1" };
        var snapshot = new LiveMedalSnapshot(
            [new(1, "示例主播", "示例牌", 10, false, null, false, [], "暂未获取")],
            Now
        );
        Assert.True(
            DailyTaskNotificationStatusSource.MedalFinished(
                snapshot,
                options,
                1001,
                Day,
                Now,
                new(new Clock())
            )
        );
        options.ExcludedAnchorIds = "";
        options.EnableLike = options.EnableDanmaku = options.EnableWatch = false;
        Assert.True(
            DailyTaskNotificationStatusSource.MedalFinished(
                null,
                options,
                1001,
                Day,
                Now,
                new(new Clock())
            )
        );
    }

    [Theory]
    [InlineData("23:55", 23, 55)]
    [InlineData("18:30", 18, 30)]
    [InlineData("24:00", 23, 55)]
    [InlineData("bad", 23, 55)]
    public void CutoffUsesValidTimeOrDefaultAndChinaCalendar(string value, int hour, int minute)
    {
        Assert.Equal(new TimeSpan(hour, minute, 0), DailyTaskNotificationSchedule.Parse(value));
        Assert.Equal(Day, DailyTaskNotificationSchedule.Day(Now));
        Assert.Equal(
            Day.AddDays(1),
            DailyTaskNotificationSchedule.Day(Now.ToUniversalTime().AddHours(6))
        );
    }

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
