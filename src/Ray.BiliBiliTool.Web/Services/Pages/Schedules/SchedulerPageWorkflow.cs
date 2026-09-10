using BlazingQuartz.Core.Models;
using BlazingQuartz.Core.Services;
using Quartz;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Extensions;

namespace Ray.BiliBiliTool.Web.Services.Pages.Schedules;

public class SchedulerPageWorkflow(
    ISchedulerService schedulerService,
    IExecutionLogService executionLogService
) : ISchedulerPageWorkflow
{
    public IAsyncEnumerable<ScheduleModel> GetAllJobsAsync(ScheduleJobFilter? filter = null) =>
        schedulerService.GetAllJobsAsync(filter);

    public Task<JobDetailModel?> GetJobDetailAsync(string? jobName, string? jobGroup)
    {
        if (string.IsNullOrEmpty(jobName))
            return Task.FromResult<JobDetailModel?>(null);
        return schedulerService.GetJobDetail(
            jobName,
            jobGroup ?? BlazingQuartz.Constants.DEFAULT_GROUP
        );
    }

    public Task<ScheduleModel> GetScheduleModelAsync(ITrigger trigger) =>
        schedulerService.GetScheduleModelAsync(trigger);

    public async Task EnrichWithLastExecutionAsync(IEnumerable<ScheduleModel> scheduleModels)
    {
        var latestResult = new PageMetadata(0, 1);
        var scheduleJobType = new HashSet<LogType> { LogType.ScheduleJob };

        foreach (ScheduleModel schModel in scheduleModels)
        {
            if (string.IsNullOrEmpty(schModel.JobName))
                continue;

            PagedList<ExecutionLog> latestLogList = await executionLogService.GetLatestExecutionLog(
                schModel.JobName,
                schModel.JobGroup,
                schModel.TriggerName,
                schModel.TriggerGroup,
                latestResult,
                logTypes: scheduleJobType
            );

            if (latestLogList != null && latestLogList.Any())
            {
                ExecutionLog latestLog = latestLogList.First();
                if (!schModel.PreviousTriggerTime.HasValue)
                    schModel.PreviousTriggerTime = latestLog.FireTimeUtc;

                if (latestLog.IsSuccess.HasValue && !latestLog.IsSuccess.Value)
                    schModel.ExceptionMessage = latestLog.GetShortResultMessage();
                else if (latestLog.IsException ?? false)
                    schModel.ExceptionMessage = latestLog.GetShortExceptionMessage();
            }
        }
    }

    public async Task<SchedulerActionResult> ResumeJobAsync(ScheduleModel model)
    {
        if (model.TriggerName == null)
            return new SchedulerActionResult(
                false,
                "Cannot resume schedule. Trigger name is null."
            );
        await schedulerService.ResumeTrigger(model.TriggerName, model.TriggerGroup);
        return new SchedulerActionResult(true, null);
    }

    public async Task<SchedulerActionResult> PauseJobAsync(ScheduleModel model)
    {
        if (model.TriggerName == null)
            return new SchedulerActionResult(false, "Cannot pause schedule. Trigger name is null.");
        await schedulerService.PauseTrigger(model.TriggerName, model.TriggerGroup);
        return new SchedulerActionResult(true, null);
    }

    public async Task<SchedulerActionResult> TriggerJobNowAsync(ScheduleModel model)
    {
        if (model.JobName == null)
            return new SchedulerActionResult(
                false,
                "Cannot add trigger. Check if job still exists."
            );
        await schedulerService.TriggerJob(model.JobName, model.JobGroup);
        return new SchedulerActionResult(true, null);
    }
}
