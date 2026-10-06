namespace Ray.BiliBiliTool.Application.Contracts.Notifications;

public sealed record TaskFailureEntry(string TaskKey, string MaskedAccount, int Count);

public sealed record TaskFailureBatchState(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset LastFailureAtUtc,
    DateTimeOffset? NotificationAttemptUtc,
    IReadOnlyList<TaskFailureEntry> Entries,
    IReadOnlyList<TaskFailureDayState>? Days = null
);

public sealed record TaskFailureDayState(
    DateOnly Day,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset LastActivityUtc,
    DateTimeOffset? NotificationAttemptUtc,
    DateTimeOffset? SentAtUtc,
    bool HasScheduledActivity,
    IReadOnlyList<TaskFailureEntry> Entries
);

public sealed record TaskFailureSummaryItem(
    string TaskName,
    IReadOnlyList<string> MaskedAccounts,
    int FailureCount,
    IReadOnlyList<string>? PendingAccounts = null,
    bool Completed = false
);

public sealed record TaskFailureSummary(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<TaskFailureSummaryItem> Items,
    DateOnly? Day = null,
    bool CutoffReached = false
);

public interface ITaskFailureNotifier
{
    Task<bool> SendAsync(TaskFailureSummary summary, CancellationToken cancellationToken);
}

public interface ITaskFailureBatchStateStore
{
    Task<TaskFailureBatchState?> ReadAsync(CancellationToken cancellationToken);
    Task WriteAsync(TaskFailureBatchState? state, CancellationToken cancellationToken);
}
