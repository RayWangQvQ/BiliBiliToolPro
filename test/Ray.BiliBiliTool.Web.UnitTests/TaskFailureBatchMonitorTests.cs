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
        Assert.Empty(f.Notifier.Messages);
        first.Dispose();
        second.Dispose();
        await f.Monitor.FlushReadyAsync();
        Assert.Empty(f.Notifier.Messages);
        f.Clock.Advance(2);
        await Task.WhenAll(f.Monitor.FlushReadyAsync(), f.Monitor.FlushReadyAsync());
        var message = Assert.Single(f.Notifier.Messages);
        Assert.Equal("BiliBili 任务失败汇总", message.Title);
        Assert.Equal(NotificationLevel.Warning, message.Level);
        Assert.Contains("每日任务", message.Body);
        Assert.Contains("漫画签到/阅读", message.Body);
        Assert.Contains("2 次失败", message.Body);
        Assert.Contains("***3456", message.Body);
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
        Assert.Empty(f.Notifier.Messages);
        f.Clock.Advance(1);
        await f.Monitor.FlushReadyAsync();
        var message = Assert.Single(f.Notifier.Messages);
        Assert.Contains("每日任务", message.Body);
        Assert.Contains("漫画签到/阅读", message.Body);
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
        Assert.Empty(f.Notifier.Messages);
        f.Config["TaskFailureNotification:Enabled"] = "false";
        f.Config[TaskFailureNotificationCatalog.SettingKey("DailyTaskAppService")] = "true";
        await f.Monitor.RecordFailureAsync(1, "DailyTaskAppService");
        Assert.Null(f.Store.State);
    }

    [Fact]
    public async Task SuccessfulBatch_ProducesNoNotification_AndLaterBatchIsIndependent()
    {
        var f = new Fixture();
        using (f.Monitor.BeginBatch()) { }
        f.Clock.Advance(2);
        await f.Monitor.FlushReadyAsync();
        Assert.Empty(f.Notifier.Messages);
        for (var i = 0; i < 2; i++)
        {
            using (f.Monitor.BeginBatch())
                await f.Monitor.RecordFailureAsync(1, "DailyTaskAppService");
            f.Clock.Advance(2);
            await f.Monitor.FlushReadyAsync();
        }
        Assert.Equal(2, f.Notifier.Messages.Count);
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
        var message = Assert.Single(f.Notifier.Messages);
        Assert.Contains("每日任务", message.Body);
        Assert.DoesNotContain("漫画签到/阅读", message.Body);
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
        var message = Assert.Single(f.Notifier.Messages);
        Assert.Contains("每日任务", message.Body);
        Assert.DoesNotContain("漫画签到/阅读", message.Body);
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

    private sealed class FakeNotifier : INotificationService
    {
        public List<NotificationMessage> Messages { get; } = [];

        public Task SendAsync(NotificationMessage message, string groupKey)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task SendSummaryAsync(string title, SummaryLine[] lines, string groupKey) =>
            Task.CompletedTask;
    }
}
