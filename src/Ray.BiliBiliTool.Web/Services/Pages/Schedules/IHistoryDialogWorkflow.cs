using BlazingQuartz.Core.Models;
using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Web.Services.Pages.Schedules;

/// <summary>
/// Web-layer contract for the HistoryDialog data access.
/// Wraps IExecutionLogService for pattern consistency with other dialog workflows (WEB-02 / D-10).
/// </summary>
public interface IHistoryDialogWorkflow
{
    Task<PagedList<ExecutionLog>> GetHistoryPageAsync(
        string jobName,
        string jobGroup,
        string? triggerName,
        string? triggerGroup,
        PageMetadata pageMetadata,
        long firstLogId
    );
}
