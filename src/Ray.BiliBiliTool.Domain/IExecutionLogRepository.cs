namespace Ray.BiliBiliTool.Domain;

public interface IExecutionLogRepository
{
    Task<string?> GetLatestRunInstanceIdAsync(string jobName, string? triggerName);
    Task<string?> GetLatestRunInstanceIdAsync(
        string jobName,
        string? triggerName,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetLatestRunInstanceIdAsync(jobName, triggerName).WaitAsync(cancellationToken);
    }
    Task<List<BiliLogs>> GetLogsForRunAsync(
        string fireInstanceId,
        int maxCount,
        CancellationToken ct
    );
}
