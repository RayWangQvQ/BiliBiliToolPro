using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class TaskFailureBatchMonitorTests
{
    [Fact]
    public async Task SeparatedRoundsWaitForAllAutomaticTasksThenSendOnceAcrossRestart()
    {
        var f = new Fixture();
        f.Source.Finished = false;
        for (var round = 0; round < 3; round++)
        {
            using (f.Monitor.BeginBatch())
                await f.Monitor.RecordFailureAsync(123456, "DailyTaskAppService");
            f.Clock.Advance(20);
            await f.Monitor.FlushReadyAsync();
        }
        Assert.Empty(f.Notifier.Summaries);
        f.Source.Finished = true;
        await Task.WhenAll(f.Monitor.FlushReadyAsync(), f.Monitor.FlushReadyAsync());
        var summary = Assert.Single(f.Notifier.Summaries);
        Assert.False(summary.CutoffReached);
        Assert.Equal(3, summary.Items[0].FailureCount);
        Assert.Equal("***3456", Assert.Single(summary.Items[0].MaskedAccounts));
        Assert.NotNull(Assert.Single(f.Store.State!.Days!).SentAtUtc);
        await f.Monitor.RecordActivityAsync();
        await f.Monitor.RecordFailureAsync(123456, "DailyTaskAppService");
        f.Clock.Advance(20);
        await f.CreateMonitor().FlushReadyAsync();
        Assert.Single(f.Notifier.Summaries);
    }

    [Fact]
    public async Task ActiveTasksAndQuietPeriodPreventPrematureCompletedSummary()
    {
        var f = new Fixture();
        var lease = f.Monitor.BeginBatch();
        await f.Monitor.RecordActivityAsync();
        f.Clock.Advance(5);
        await f.Monitor.FlushReadyAsync();
        Assert.Empty(f.Notifier.Summaries);
        lease.Dispose();
        f.Clock.Advance(1);
        await f.Monitor.FlushReadyAsync();
        Assert.Empty(f.Notifier.Summaries);
        f.Clock.Advance(1);
        await f.Monitor.FlushReadyAsync();
        Assert.True(Assert.Single(f.Notifier.Summaries).Items[0].Completed);
    }

    [Fact]
    public async Task ConfiguredCutoffSendsPendingAndFailedItemsEvenWhileRunningOnlyOnce()
    {
        var f = new Fixture();
        f.Config[DailyTaskNotificationSchedule.CutoffKey] = "08:10";
        f.Source.Finished = false;
        f.Source.Running = true;
        using var lease = f.Monitor.BeginBatch();
        await f.Monitor.RecordFailureAsync(123456, "DailyTaskAppService");
        f.Clock.Advance(9);
        await f.Monitor.FlushReadyAsync();
        Assert.Empty(f.Notifier.Summaries);
        f.Clock.Advance(1);
        await f.Monitor.FlushReadyAsync();
        var summary = Assert.Single(f.Notifier.Summaries);
        Assert.True(summary.CutoffReached);
        Assert.Equal("***3456", Assert.Single(summary.Items[0].PendingAccounts!));
        Assert.Equal(1, summary.Items[0].FailureCount);
        f.Source.Finished = true;
        f.Source.Running = false;
        f.Clock.Advance(10);
        await f.CreateMonitor().FlushReadyAsync();
        Assert.Single(f.Notifier.Summaries);
    }

    [Fact]
    public async Task CutoffAlsoReportsMissedScheduledTasksWithoutRecordedActivity()
    {
        var f = new Fixture();
        f.Source.Finished = false;
        f.Config[DailyTaskNotificationSchedule.CutoffKey] = "08:00";
        await f.Monitor.FlushReadyAsync();
        Assert.True(Assert.Single(f.Notifier.Summaries).CutoffReached);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnconfirmedDeliveryDoesNotRepeatAfterTimeoutOrRestart(bool throws)
    {
        var f = new Fixture();
        f.Notifier.Accept = false;
        f.Notifier.Throw = throws;
        await f.Monitor.RecordActivityAsync();
        f.Clock.Advance(2);
        await f.Monitor.FlushReadyAsync();
        Assert.NotNull(Assert.Single(f.Store.State!.Days!).NotificationAttemptUtc);
        Assert.Null(Assert.Single(f.Store.State.Days!).SentAtUtc);
        f.Clock.Advance(30);
        f.Notifier.Accept = true;
        f.Notifier.Throw = false;
        await f.CreateMonitor().FlushReadyAsync();
        Assert.Single(f.Notifier.Summaries);
    }

    [Fact]
    public async Task NextChinaDayHasIndependentSummaryAndDoesNotCarryOldFailures()
    {
        var f = new Fixture();
        await f.Monitor.RecordFailureAsync(123456, "DailyTaskAppService");
        f.Clock.Advance(2);
        await f.Monitor.FlushReadyAsync();
        f.Clock.Advance(24 * 60);
        var next = f.CreateMonitor();
        await next.RecordActivityAsync();
        f.Clock.Advance(2);
        await next.FlushReadyAsync();
        Assert.Equal(2, f.Notifier.Summaries.Count);
        Assert.Equal(new DateOnly(2026, 10, 6), f.Notifier.Summaries[1].Day);
        Assert.Equal(0, f.Notifier.Summaries[1].Items[0].FailureCount);
        Assert.Equal(2, f.Store.State!.Days!.Count);
    }

    [Fact]
    public async Task TaskAndGlobalSwitchesImmediatelyFilterOrStopDailySummary()
    {
        var f = new Fixture();
        await f.Monitor.RecordFailureAsync(123456, "DailyTaskAppService");
        f.Clock.Advance(2);
        f.Config[TaskFailureNotificationCatalog.SettingKey("DailyTaskAppService")] = "false";
        await f.Monitor.FlushReadyAsync();
        Assert.Empty(f.Notifier.Summaries);
        Assert.Null(Assert.Single(f.Store.State!.Days!).NotificationAttemptUtc);
        f.Config[TaskFailureNotificationCatalog.SettingKey("DailyTaskAppService")] = "true";
        f.Config["TaskFailureNotification:Enabled"] = "false";
        await f.Monitor.FlushReadyAsync();
        Assert.Empty(f.Notifier.Summaries);
        f.Config["TaskFailureNotification:Enabled"] = "true";
        await f.Monitor.FlushReadyAsync();
        Assert.Single(f.Notifier.Summaries);
    }

    [Fact]
    public async Task MissingSendKeyDoesNotConsumeDailyAttempt()
    {
        var f = new Fixture();
        f.Config["CookieCheck:ServerChanSendKey"] = "";
        await f.Monitor.RecordActivityAsync();
        f.Clock.Advance(2);
        await f.Monitor.FlushReadyAsync();
        Assert.Null(Assert.Single(f.Store.State!.Days!).NotificationAttemptUtc);
        f.Config["CookieCheck:ServerChanSendKey"] = "SCT123synthetic";
        await f.Monitor.FlushReadyAsync();
        Assert.Single(f.Notifier.Summaries);
    }

    [Fact]
    public async Task SuppressedManualAndRecoveryOperationsDoNotCreateDailyActivity()
    {
        var f = new Fixture();
        using (new TaskFailureNotificationScope(suppress: true))
        {
            await f.Monitor.RecordActivityAsync();
            await f.Monitor.RecordFailureAsync(123456, "DailyTaskAppService");
        }
        Assert.Null(f.Store.State);
        f.Clock.Advance(2);
        await f.Monitor.FlushReadyAsync();
        f.Config[DailyTaskNotificationSchedule.CutoffKey] = "08:00";
        await f.Monitor.FlushReadyAsync();
        Assert.Empty(f.Notifier.Summaries);
    }

    [Fact]
    public async Task LegacyPendingStateMigratesAndPreservesFailureCounts()
    {
        var f = new Fixture();
        await f.Store.WriteAsync(
            new(
                f.Clock.GetUtcNow(),
                f.Clock.GetUtcNow(),
                null,
                [new("DailyTaskAppService", "***3456", 2)]
            ),
            default
        );
        f.Clock.Advance(2);
        await f.Monitor.FlushReadyAsync();
        Assert.Equal(2, Assert.Single(f.Notifier.Summaries).Items[0].FailureCount);
        Assert.NotNull(Assert.Single(f.Store.State!.Days!).SentAtUtc);
    }

    [Fact]
    public async Task UnknownTasksAreNotRecorded()
    {
        var f = new Fixture();
        await f.Monitor.RecordFailureAsync(123456, "synthetic-unknown-task");
        Assert.Null(f.Store.State);
    }

    [Fact]
    public async Task ConcurrentRecoveryAndScheduledTasksKeepNotificationContextsSeparate()
    {
        var f = new Fixture();
        await Task.WhenAll(
            Task.Run(async () =>
            {
                using var scope = new TaskFailureNotificationScope(suppress: true);
                await f.Monitor.RecordActivityAsync();
                await f.Monitor.RecordFailureAsync(654321, "DailyTaskAppService");
            }),
            Task.Run(() => f.Monitor.RecordFailureAsync(123456, "DailyTaskAppService"))
        );
        f.Clock.Advance(2);
        await f.Monitor.FlushReadyAsync();
        Assert.Equal(
            "***3456",
            Assert.Single(Assert.Single(f.Notifier.Summaries).Items[0].MaskedAccounts)
        );
    }

    [Fact]
    public void NestedScopesRestorePriorContextAndCannotReenableSuppressedReminders()
    {
        using (new TaskFailureNotificationScope(suppress: true))
        {
            using (new TaskFailureNotificationScope(suppress: false))
                Assert.True(TaskFailureNotificationScope.IsSuppressed);
            Assert.True(TaskFailureNotificationScope.IsSuppressed);
        }
        Assert.False(TaskFailureNotificationScope.IsSuppressed);
    }

    private sealed class Fixture
    {
        public IConfigurationRoot Config { get; } =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["CookieCheck:ServerChanSendKey"] = "SCT123synthetic",
                    }
                )
                .Build();
        public MemoryStore Store { get; } = new();
        public FakeNotifier Notifier { get; } = new();
        public FakeClock Clock { get; } = new();
        public FakeSource Source { get; } = new();
        public TaskFailureBatchMonitor Monitor { get; }

        public Fixture() => Monitor = CreateMonitor();

        public TaskFailureBatchMonitor CreateMonitor() =>
            new(
                Config,
                Store,
                Notifier,
                Clock,
                NullLogger<TaskFailureBatchMonitor>.Instance,
                Source
            );
    }

    private sealed class FakeSource : IDailyTaskNotificationStatusSource
    {
        public bool Finished { get; set; } = true;
        public bool Running { get; set; }

        public Task<DailyTaskNotificationStatus> ReadAsync(
            DateOnly day,
            DateTimeOffset now,
            CancellationToken token
        ) =>
            Task.FromResult(
                new DailyTaskNotificationStatus(
                    Finished,
                    Running,
                    [
                        new(
                            "DailyTaskAppService",
                            "每日任务",
                            ["***3456"],
                            Finished ? [] : ["***3456"]
                        ),
                    ]
                )
            );
    }

    private sealed class FakeClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 5, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(int minutes) => _now += TimeSpan.FromMinutes(minutes);
    }

    private sealed class MemoryStore : ITaskFailureBatchStateStore
    {
        public TaskFailureBatchState? State { get; private set; }

        public Task<TaskFailureBatchState?> ReadAsync(CancellationToken token) =>
            Task.FromResult(State);

        public Task WriteAsync(TaskFailureBatchState? state, CancellationToken token)
        {
            State = state;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNotifier : ITaskFailureNotifier
    {
        public bool Accept { get; set; } = true;
        public bool Throw { get; set; }
        public List<TaskFailureSummary> Summaries { get; } = [];

        public Task<bool> SendAsync(TaskFailureSummary summary, CancellationToken token)
        {
            Summaries.Add(summary);
            if (Throw)
                throw new HttpRequestException("synthetic ambiguous response");
            return Task.FromResult(Accept);
        }
    }
}
