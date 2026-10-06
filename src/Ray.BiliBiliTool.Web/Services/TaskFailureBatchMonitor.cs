using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Infrastructure.Notifications;

namespace Ray.BiliBiliTool.Web.Services;

public interface ITaskFailureBatchMonitor
{
    IDisposable BeginBatch();
    Task RecordActivityAsync(CancellationToken token = default) => Task.CompletedTask;
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
    ILogger<TaskFailureBatchMonitor> logger,
    IDailyTaskNotificationStatusSource? statusSource = null
) : ITaskFailureBatchMonitor
{
    public static readonly TimeSpan MergeDelay = TimeSpan.FromMinutes(2);
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

    private static List<TaskFailureDayState> Days(TaskFailureBatchState? state) =>
        state is null
            ? []
            : state.Days?.ToList()
                ??
                [
                    new(
                        DailyTaskNotificationSchedule.Day(state.StartedAtUtc),
                        state.StartedAtUtc,
                        state.LastFailureAtUtc,
                        state.NotificationAttemptUtc,
                        null,
                        true,
                        state.Entries
                    ),
                ];

    private static TaskFailureDayState NewDay(DateOnly day, DateTimeOffset now) =>
        new(day, now, now, null, null, false, []);

    private async Task WriteAsync(List<TaskFailureDayState> days, CancellationToken token)
    {
        var today = DailyTaskNotificationSchedule.Day(timeProvider.GetUtcNow());
        days = days.Where(day => day.Day >= today.AddDays(-7)).OrderBy(day => day.Day).ToList();
        var latest = days.Last();
        await store.WriteAsync(
            new(
                latest.StartedAtUtc,
                latest.LastActivityUtc,
                latest.NotificationAttemptUtc,
                latest.Entries,
                days
            ),
            token
        );
    }

    public async Task RecordActivityAsync(CancellationToken token = default)
    {
        if (
            TaskFailureNotificationScope.IsSuppressed
            || !configuration.GetValue("TaskFailureNotification:Enabled", true)
        )
            return;
        await _gate.WaitAsync(token);
        try
        {
            var now = timeProvider.GetUtcNow();
            var day = DailyTaskNotificationSchedule.Day(now);
            var days = Days(await store.ReadAsync(token));
            var index = days.FindIndex(item => item.Day == day);
            if (index < 0)
            {
                days.Add(NewDay(day, now));
                index = days.Count - 1;
            }
            days[index] = days[index] with { HasScheduledActivity = true, LastActivityUtc = now };
            await WriteAsync(days, token);
        }
        finally
        {
            _gate.Release();
        }
    }

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
            var day = DailyTaskNotificationSchedule.Day(now);
            var days = Days(await store.ReadAsync(cancellationToken));
            var index = days.FindIndex(item => item.Day == day);
            if (index < 0)
            {
                days.Add(NewDay(day, now));
                index = days.Count - 1;
            }
            var state = days[index];
            if (state.NotificationAttemptUtc is not null)
                return;
            var masked = userId is null
                ? "任务启动"
                : DailyTaskNotificationSchedule.Mask(userId.Value);
            var entries = state.Entries.ToList();
            var entry = entries.FindIndex(item =>
                item.TaskKey == taskKey && item.MaskedAccount == masked
            );
            if (entry < 0)
                entries.Add(new(taskKey, masked, 1));
            else
                entries[entry] = entries[entry] with { Count = entries[entry].Count + 1 };
            days[index] = state with
            {
                HasScheduledActivity = true,
                LastActivityUtc = now,
                Entries = entries,
            };
            await WriteAsync(days, cancellationToken);
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
            if (
                !configuration.GetValue("TaskFailureNotification:Enabled", true)
                || statusSource is null
                || ServerChanCookieExpiryNotifier.CreateEndpoint(
                    ServerChanCookieExpiryNotifier.GetSendKey(configuration)
                )
                    is null
            )
                return;
            var now = timeProvider.GetUtcNow();
            var today = DailyTaskNotificationSchedule.Day(now);
            var days = Days(await store.ReadAsync(cancellationToken));
            var candidates = new[] { today };
            foreach (var day in candidates)
            {
                var index = days.FindIndex(item => item.Day == day);
                var state = index >= 0 ? days[index] : NewDay(day, now);
                if (state.NotificationAttemptUtc is not null)
                    continue;
                var cutoff =
                    new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(8))
                    + DailyTaskNotificationSchedule.Parse(
                        configuration[DailyTaskNotificationSchedule.CutoffKey]
                    );
                var finalTime = now >= cutoff;
                var status = await statusSource.ReadAsync(day, now, cancellationToken);
                if (!finalTime)
                {
                    lock (_activityGate)
                        if (
                            _active > 0
                            || now - _lastActivity < MergeDelay
                            || now - state.LastActivityUtc < MergeDelay
                        )
                            continue;
                    if (!state.HasScheduledActivity || !status.AllFinished || status.Running)
                        continue;
                }
                var entries = state.Entries.Where(entry => IsEnabled(entry.TaskKey)).ToArray();
                var items = status
                    .Items.Where(item => IsEnabled(item.TaskKey))
                    .Select(item =>
                    {
                        var failed = entries
                            .Where(entry => entry.TaskKey == item.TaskKey)
                            .ToArray();
                        return new TaskFailureSummaryItem(
                            item.DisplayName,
                            failed.Select(entry => entry.MaskedAccount).Distinct().ToArray(),
                            failed.Sum(entry => entry.Count),
                            item.PendingAccounts,
                            item.PendingAccounts.Count == 0
                        );
                    })
                    .ToList();
                foreach (
                    var missing in entries
                        .Where(entry => !status.Items.Any(item => item.TaskKey == entry.TaskKey))
                        .GroupBy(entry => entry.TaskKey)
                )
                    items.Add(
                        new(
                            TaskFailureNotificationCatalog
                                .All.First(task => task.TaskKey == missing.Key)
                                .DisplayName,
                            missing.Select(entry => entry.MaskedAccount).Distinct().ToArray(),
                            missing.Sum(entry => entry.Count)
                        )
                    );
                if (
                    items.Count == 0
                    || (
                        !state.HasScheduledActivity
                        && items.All(item => item.Completed && item.FailureCount == 0)
                    )
                )
                    continue;
                // Persist before sending; an ambiguous HTTP result must not create repeated pushes.
                state = state with
                {
                    NotificationAttemptUtc = now,
                };
                if (index < 0)
                {
                    days.Add(state);
                    index = days.Count - 1;
                }
                else
                    days[index] = state;
                await WriteAsync(days, cancellationToken);
                if (
                    await notifier.SendAsync(
                        new(state.StartedAtUtc, now, items, day, finalTime),
                        cancellationToken
                    )
                )
                {
                    days[index] = state with { SentAtUtc = now };
                    await WriteAsync(days, cancellationToken);
                }
                else
                    logger.LogWarning("今日任务汇总发送未确认，请检查 Server 酱通知配置");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            logger.LogWarning("今日任务汇总暂时无法处理，请检查通知配置与执行记录");
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
