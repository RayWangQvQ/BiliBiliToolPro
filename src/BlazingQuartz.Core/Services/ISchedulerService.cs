using BlazingQuartz.Core.Models;
using Quartz;

namespace BlazingQuartz.Core.Services
{
    public interface ISchedulerService
    {
        Task<ScheduleModel> GetScheduleModelAsync(ITrigger trigger);
        IAsyncEnumerable<ScheduleModel> GetAllJobsAsync(ScheduleJobFilter? filter = null);
        Task CreateSchedule(JobDetailModel jobDetailModel, TriggerDetailModel triggerDetailModel);
        Task<IReadOnlyCollection<string>> GetJobGroups();
        Task<IReadOnlyCollection<string>> GetTriggerGroups();
        Task<JobDetailModel?> GetJobDetail(string jobName, string groupName);
        Task<TriggerDetailModel?> GetTriggerDetail(string triggerName, string triggerGroup);
        Task<bool> ContainsTriggerKey(string triggerName, string triggerGroup);
        Task<bool> ContainsJobKey(string jobName, string jobGroup);
        Task<IReadOnlyCollection<string>> GetCalendarNames(CancellationToken cancelToken = default);
        Task PauseTrigger(string triggerName, string? triggerGroup);
        Task ResumeTrigger(string triggerName, string? triggerGroup);
        Task TriggerJob(string jobName, string jobGroup);
        Task<bool> DeleteSchedule(ScheduleModel model);
        Task UpdateSchedule(
            Key oldJobKey,
            Key? oldTriggerKey,
            JobDetailModel newJobModel,
            TriggerDetailModel newTriggerModel
        );
        Task<SchedulerMetaData> GetMetadataAsync();
        Task<IList<KeyValuePair<string, int>>> GetScheduledJobSummary();
        Task PauseAllSchedules();
        Task ResumeAllSchedules();
        Task ShutdownScheduler();
        Task StartScheduler();
        Task StandbyScheduler();
    }
}
