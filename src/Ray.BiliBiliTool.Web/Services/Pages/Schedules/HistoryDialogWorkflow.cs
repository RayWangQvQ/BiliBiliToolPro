using BlazingQuartz.Core.Models;
using BlazingQuartz.Core.Services;
using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Web.Services.Pages.Schedules;

public class HistoryDialogWorkflow(IExecutionLogService executionLogService)
    : IHistoryDialogWorkflow
{
    public Task<PagedList<ExecutionLog>> GetHistoryPageAsync(
        string jobName,
        string jobGroup,
        string? triggerName,
        string? triggerGroup,
        PageMetadata pageMetadata,
        long firstLogId
    ) =>
        executionLogService.GetLatestExecutionLog(
            jobName,
            jobGroup,
            triggerName,
            triggerGroup,
            pageMetadata,
            firstLogId
        );
}
