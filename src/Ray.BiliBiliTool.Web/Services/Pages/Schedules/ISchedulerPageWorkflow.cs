using BlazingQuartz.Core.Models;
using BlazingQuartz.Core.Services;
using Quartz;

namespace Ray.BiliBiliTool.Web.Services.Pages.Schedules;

/// <summary>
/// Web-layer contract for the Schedules page orchestration.
/// Wraps ISchedulerService and IExecutionLogService so the component
/// code-behind injects only Web-layer services (WEB-02 / D-01 / D-02).
/// </summary>
public interface ISchedulerPageWorkflow
{
    // Data loading
    IAsyncEnumerable<ScheduleModel> GetAllJobsAsync(ScheduleJobFilter? filter = null);
    Task<JobDetailModel?> GetJobDetailAsync(string? jobName, string? jobGroup);
    Task<ScheduleModel> GetScheduleModelAsync(ITrigger trigger);

    // Log enrichment (D-01)
    Task EnrichWithLastExecutionAsync(IEnumerable<ScheduleModel> scheduleModels);

    // Scheduler actions (D-02)
    Task<SchedulerActionResult> ResumeJobAsync(ScheduleModel model);
    Task<SchedulerActionResult> PauseJobAsync(ScheduleModel model);
    Task<SchedulerActionResult> TriggerJobNowAsync(ScheduleModel model);
}
