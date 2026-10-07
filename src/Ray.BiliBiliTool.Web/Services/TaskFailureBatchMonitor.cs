using Ray.BiliBiliTool.Application.Contracts.Notifications;

namespace Ray.BiliBiliTool.Web.Services;

public interface ITaskFailureBatchMonitor
{
    IDisposable BeginBatch();
    Task RecordFailureAsync(
        long? userId,
        string taskKey,
        CancellationToken cancellationToken = default
    );
    Task FlushReadyAsync(CancellationToken cancellationToken = default);
}

public sealed class TaskFailureBatchMonitor(
    IConfiguration configuration,
    ITaskFailureBatchStateStore store,
    ITaskFailureNotifier notifier,
    TimeProvider timeProvider,
    ILogger<TaskFailureBatchMonitor> logger
) : ITaskFailureBatchMonitor
{
    public static readonly TimeSpan MergeDelay = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(15);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _activityGate = new();
    private int _active;
    private DateTimeOffset _lastActivity;

    public IDisposable BeginBatch()
    {
        lock (_activityGate)
        {
            _active++;
            _lastActivity = timeProvider.GetUtcNow();
        }
        return new BatchLease(this);
    }

    private void EndBatch()
    {
        lock (_activityGate)
        {
            _active--;
            _lastActivity = timeProvider.GetUtcNow();
        }
    }

    private bool IsEnabled(string taskKey) =>
        configuration.GetValue("TaskFailureNotification:Enabled", true)
        && TaskFailureNotificationCatalog.All.Any(task => task.TaskKey == taskKey)
        && configuration.GetValue(TaskFailureNotificationCatalog.SettingKey(taskKey), true);

    public async Task RecordFailureAsync(
        long? userId,
        string taskKey,
        CancellationToken cancellationToken = default
    )
    {
        if (TaskFailureNotificationScope.IsSuppressed || !IsEnabled(taskKey))
            return;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            var state = await store.ReadAsync(cancellationToken);
            var account = userId?.ToString();
            var maskedAccount = account is null
                ? "任务启动"
                : "***" + account[^Math.Min(4, account.Length)..];
            var entries = state?.Entries.ToList() ?? [];
            var index = entries.FindIndex(entry =>
                entry.TaskKey == taskKey && entry.MaskedAccount == maskedAccount
            );
            if (index < 0)
                entries.Add(new(taskKey, maskedAccount, 1));
            else
                entries[index] = entries[index] with { Count = entries[index].Count + 1 };
            await store.WriteAsync(
                new(state?.StartedAtUtc ?? now, now, state?.NotificationAttemptUtc, entries),
                cancellationToken
            );
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task FlushReadyAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var state = await store.ReadAsync(cancellationToken);
            if (state is null)
                return;
            var now = timeProvider.GetUtcNow();
            lock (_activityGate)
            {
                if (
                    _active > 0
                    || now - _lastActivity < MergeDelay
                    || now - state.LastFailureAtUtc < MergeDelay
                )
                    return;
            }
            var entries = state.Entries.Where(entry => IsEnabled(entry.TaskKey)).ToList();
            if (entries.Count == 0)
            {
                await store.WriteAsync(null, cancellationToken);
                return;
            }
            if (state.NotificationAttemptUtc is { } attempt && now - attempt < RetryDelay)
                return;
            state = state with { NotificationAttemptUtc = now, Entries = entries };
            await store.WriteAsync(state, cancellationToken);
            var items = entries
                .GroupBy(entry => entry.TaskKey)
                .Select(group => new TaskFailureSummaryItem(
                    TaskFailureNotificationCatalog
                        .All.First(task => task.TaskKey == group.Key)
                        .DisplayName,
                    group.Select(entry => entry.MaskedAccount).Distinct().ToArray(),
                    group.Sum(entry => entry.Count)
                ))
                .ToArray();
            if (await notifier.SendAsync(new(state.StartedAtUtc, now, items), cancellationToken))
                await store.WriteAsync(null, cancellationToken);
            else
                logger.LogWarning("任务失败汇总未发送，请检查 Server 酱通知配置。稍后自动重试");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Provider exception messages can contain the SendKey.
            logger.LogWarning("任务失败汇总发送失败，稍后自动重试");
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed class BatchLease(TaskFailureBatchMonitor owner) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                owner.EndBatch();
        }
    }
}
