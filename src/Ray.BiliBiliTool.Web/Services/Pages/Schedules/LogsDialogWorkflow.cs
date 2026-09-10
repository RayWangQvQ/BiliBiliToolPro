using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Web.Services.Pages.Schedules;

public class LogsDialogWorkflow(IExecutionLogRepository logRepository) : ILogsDialogWorkflow
{
    public Task<string?> GetLatestRunInstanceIdAsync(string jobName, string triggerName) =>
        logRepository.GetLatestRunInstanceIdAsync(jobName, triggerName);

    public Task<List<BiliLogs>> GetLogsForRunAsync(
        string fireInstanceId,
        int maxCount,
        CancellationToken ct
    ) => logRepository.GetLogsForRunAsync(fireInstanceId, maxCount, ct);
}
