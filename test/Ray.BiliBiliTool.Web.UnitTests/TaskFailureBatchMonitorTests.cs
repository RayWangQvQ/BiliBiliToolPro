using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Application.Contracts.Notifications;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class TaskFailureBatchMonitorTests
{
    [Fact]
    public async Task ConcurrentTasksAndAccounts_SendOneSummaryAfterLastTaskEnds()
    {
        var f = new Fixture();
        using var first = f.Monitor.BeginBatch();
        var second = f.Monitor.BeginBatch();
        await Task.WhenAll(
            f.Monitor.RecordFailureAsync(123456, "DailyTaskAppService"),
            f.Monitor.RecordFailureAsync(654321, "MangaTaskAppService"),
            f.Monitor.RecordFailureAsync(123456, "DailyTaskAppService")
        );
        f.Clock.Advance(10);
        await f.Monitor.FlushReadyAsync();
        Assert.Empty(f.Notifier.Summaries);
        first.Dispose();
        second.Dispose();
        await f.Monitor.FlushReadyAsync();
        Assert.Empty(f.Notifier.Summaries);
        f.Clock.Advance(2);
        await Task.WhenAll(f.Monitor.FlushReadyAsync(), f.Monitor.FlushReadyAsync());
        var summary = Assert.Single(f.Notifier.Summaries);
        Assert.Equal(2, summary.Items.Count);
        var daily = summary.Items.Single(item => item.TaskName == "每日任务");
        Assert.Equal(2, daily.FailureCount);
        Assert.Equal("***3456", Assert.Single(daily.MaskedAccounts));
        Assert.Null(f.Store.State);
    }

    [Fact]
    public async Task AdjacentTask_StartsBeforeMergeDelay_StaysInSameBatch()
    {
        var f = new Fixture();
        using (f.Monitor.BeginBatch())
            await f.Monitor.RecordFailureAsync(1, "DailyTaskAppService");
        f.Clock.Advance(1);
        using (f.Monitor.BeginBatch())
            await f.Monitor.RecordFailureAsync(2, "MangaTaskAppService");
        f.Clock.Advance(1);
        await f.Monitor.FlushReadyAsync();
        Assert.Empty(f.Notifier.Summaries);
        f.Clock.Advance(1);
        await f.Monitor.FlushReadyAsync();
        Assert.Equal(2, Assert.Single(f.Notifier.Summaries).Items.Count);
    }

    [Fact]
    public async Task PerTaskSwitchAndGlobalSwitch_ExcludeFailuresAndTakeEffectImmediately()
    {
        var f = new Fixture();
        f.Config[TaskFailureNotificationCatalog.SettingKey("MangaTaskAppService")] = "false";
        await f.Monitor.RecordFailureAsync(1, "MangaTaskAppService");
        Assert.Null(f.Store.State);
        await f.Monitor.RecordFailureAsync(1, "DailyTaskAppService");
        f.Config[TaskFailureNotificationCatalog.SettingKey("DailyTaskAppService")] = "false";
        f.Clock.Advance(2);
        await f.Monitor.FlushReadyAsync();
        Assert.Null(f.Store.State);
        Assert.Empty(f.Notifier.Summaries);
        f.Config["TaskFailureNotification:Enabled"] = "false";
        f.Config[TaskFailureNotificationCatalog.SettingKey("DailyTaskAppService")] = "true";
        await f.Monitor.RecordFailureAsync(1, "DailyTaskAppService");
        Assert.Null(f.Store.State);
    }

    [Fact]
    public async Task FailedDelivery_RemainsPendingAndRetriesAfterBackoffAcrossRestart()
    {
        var f = new Fixture();
        f.Notifier.Accept = false;
        await f.Monitor.RecordFailureAsync(1, "DailyTaskAppService");
        f.Clock.Advance(2);
        await f.Monitor.FlushReadyAsync();
        Assert.NotNull(f.Store.State);
        await f.CreateMonitor().FlushReadyAsync();
        Assert.Single(f.Notifier.Summaries);
        f.Clock.Advance(15);
        f.Notifier.Accept = true;
        await f.CreateMonitor().FlushReadyAsync();
        Assert.Equal(2, f.Notifier.Summaries.Count);
        Assert.Null(f.Store.State);
        await f.CreateMonitor().FlushReadyAsync();
        Assert.Equal(2, f.Notifier.Summaries.Count);
    }

    [Fact]
    public async Task SuccessfulBatch_ProducesNoNotification_AndLaterBatchIsIndependent()
    {
        var f = new Fixture();
        using (f.Monitor.BeginBatch()) { }
        f.Clock.Advance(2);
        await f.Monitor.FlushReadyAsync();
        Assert.Empty(f.Notifier.Summaries);
        for (var i = 0; i < 2; i++)
        {
            using (f.Monitor.BeginBatch())
                await f.Monitor.RecordFailureAsync(1, "DailyTaskAppService");
            f.Clock.Advance(2);
            await f.Monitor.FlushReadyAsync();
        }
        Assert.Equal(2, f.Notifier.Summaries.Count);
    }

    [Fact]
    public async Task UnknownTasks_AreNotForwardedToNotification()
    {
        var f = new Fixture();
        await f.Monitor.RecordFailureAsync(1, "secret-in-untrusted-task-name");
        Assert.Null(f.Store.State);
    }

    [Fact]
    public async Task SuppressedOperations_DoNotChangeAnExistingScheduledSummary()
    {
        var f = new Fixture();
        using (f.Monitor.BeginBatch())
            await f.Monitor.RecordFailureAsync(1, "DailyTaskAppService");
        var scheduledState = f.Store.State;
        f.Clock.Advance(1);
        using (new TaskFailureNotificationScope(suppress: true))
        {
            await f.Monitor.RecordFailureAsync(2, "MangaTaskAppService");
            await f.Monitor.RecordFailureAsync(1, "DailyTaskAppService");
        }
        Assert.Equal(scheduledState, f.Store.State);
        f.Clock.Advance(1);
        await f.Monitor.FlushReadyAsync();
        var item = Assert.Single(Assert.Single(f.Notifier.Summaries).Items);
        Assert.Equal("每日任务", item.TaskName);
        Assert.Equal(1, item.FailureCount);
    }

    [Fact]
    public async Task ConcurrentRecoveryAndScheduledTasks_KeepNotificationContextsSeparate()
    {
        var f = new Fixture();
        var recoveryStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var finishRecovery = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var recovery = Task.Run(async () =>
        {
            using var scope = new TaskFailureNotificationScope(suppress: true);
            recoveryStarted.SetResult();
            await finishRecovery.Task;
            await f.Monitor.RecordFailureAsync(2, "MangaTaskAppService");
        });
        await recoveryStarted.Task;
        await f.Monitor.RecordFailureAsync(1, "DailyTaskAppService");
        finishRecovery.SetResult();
        await recovery;
        f.Clock.Advance(2);
        await f.Monitor.FlushReadyAsync();
        Assert.Equal("每日任务", Assert.Single(Assert.Single(f.Notifier.Summaries).Items).TaskName);
    }

    [Fact]
    public void NestedScopes_RestorePriorContextAndCannotReenableSuppressedReminders()
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
            new ConfigurationBuilder().AddInMemoryCollection().Build();
        public MemoryStore Store { get; } = new();
        public FakeNotifier Notifier { get; } = new();
        public FakeClock Clock { get; } = new();
        public TaskFailureBatchMonitor Monitor { get; }

        public Fixture() => Monitor = CreateMonitor();

        public TaskFailureBatchMonitor CreateMonitor() =>
            new(Config, Store, Notifier, Clock, NullLogger<TaskFailureBatchMonitor>.Instance);
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
        public List<TaskFailureSummary> Summaries { get; } = [];

        public Task<bool> SendAsync(TaskFailureSummary summary, CancellationToken token)
        {
            Summaries.Add(summary);
            return Task.FromResult(Accept);
        }
    }
}
