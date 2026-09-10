namespace Ray.BiliBiliTool.Domain;

public interface IExecutionLogRepository
{
    Task<string?> GetLatestRunInstanceIdAsync(string jobName, string triggerName);
    Task<List<BiliLogs>> GetLogsForRunAsync(
        string fireInstanceId,
        int maxCount,
        CancellationToken ct
    );
}
