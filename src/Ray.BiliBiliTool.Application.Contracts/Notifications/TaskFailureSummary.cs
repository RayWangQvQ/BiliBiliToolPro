namespace Ray.BiliBiliTool.Application.Contracts.Notifications;

public sealed record TaskFailureEntry(string TaskKey, string MaskedAccount, int Count);

public sealed record TaskFailureBatchState(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset LastFailureAtUtc,
    DateTimeOffset? NotificationAttemptUtc,
    IReadOnlyList<TaskFailureEntry> Entries
);

public sealed record TaskFailureSummaryItem(
    string TaskName,
    IReadOnlyList<string> MaskedAccounts,
    int FailureCount
);

public interface ITaskFailureBatchStateStore
{
    Task<TaskFailureBatchState?> ReadAsync(CancellationToken cancellationToken);
    Task WriteAsync(TaskFailureBatchState? state, CancellationToken cancellationToken);
}
