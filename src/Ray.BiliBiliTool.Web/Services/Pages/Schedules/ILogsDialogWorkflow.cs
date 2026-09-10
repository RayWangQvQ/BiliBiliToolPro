using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Web.Services.Pages.Schedules;

/// <summary>
/// Web-layer contract for the LogsDialog data access.
/// Wraps IExecutionLogRepository so the component does not inject Domain interfaces (WEB-02 / D-05).
/// </summary>
public interface ILogsDialogWorkflow
{
    Task<string?> GetLatestRunInstanceIdAsync(string jobName, string triggerName);
    Task<List<BiliLogs>> GetLogsForRunAsync(
        string fireInstanceId,
        int maxCount,
        CancellationToken ct
    );
}
