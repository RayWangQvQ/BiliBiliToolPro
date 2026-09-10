using BlazingQuartz.Core.Models;
using FluentAssertions;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests.Schedules;

public class SchedulerPageWorkflowTests
{
    private static SchedulerPageWorkflow CreateWorkflow(
        FakeSchedulerService? scheduler = null,
        FakeExecutionLogService? logService = null
    ) =>
        new SchedulerPageWorkflow(
            scheduler ?? new FakeSchedulerService(),
            logService ?? new FakeExecutionLogService()
        );

    private static async IAsyncEnumerable<T> EmptyAsync<T>()
    {
        yield break;
    }

    [Fact]
    public async Task ResumeJobAsync_NullTriggerName_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var model = new ScheduleModel { TriggerName = null };

        var result = await workflow.ResumeJobAsync(model);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cannot resume schedule. Trigger name is null.");
    }

    [Fact]
    public async Task ResumeJobAsync_ValidTriggerName_CallsSchedulerServiceAndReturnsSuccess()
    {
        var fakeScheduler = new FakeSchedulerService();
        var workflow = CreateWorkflow(scheduler: fakeScheduler);
        var model = new ScheduleModel { TriggerName = "trigger1", TriggerGroup = "DEFAULT" };

        var result = await workflow.ResumeJobAsync(model);

        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        fakeScheduler.ResumedTriggerName.Should().Be("trigger1");
    }

    // --- PauseJobAsync ---

    [Fact]
    public async Task PauseJobAsync_NullTriggerName_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var model = new ScheduleModel { TriggerName = null };

        var result = await workflow.PauseJobAsync(model);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cannot pause schedule. Trigger name is null.");
    }

    [Fact]
    public async Task PauseJobAsync_ValidTriggerName_CallsSchedulerServiceAndReturnsSuccess()
    {
        var fakeScheduler = new FakeSchedulerService();
        var workflow = CreateWorkflow(scheduler: fakeScheduler);
        var model = new ScheduleModel { TriggerName = "trigger1", TriggerGroup = "DEFAULT" };

        var result = await workflow.PauseJobAsync(model);

        result.IsSuccess.Should().BeTrue();
        fakeScheduler.PausedTriggerName.Should().Be("trigger1");
    }

    // --- TriggerJobNowAsync ---

    [Fact]
    public async Task TriggerJobNowAsync_NullJobName_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var model = new ScheduleModel { JobName = null };

        var result = await workflow.TriggerJobNowAsync(model);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cannot add trigger. Check if job still exists.");
    }

    [Fact]
    public async Task TriggerJobNowAsync_ValidJobName_CallsSchedulerServiceAndReturnsSuccess()
    {
        var fakeScheduler = new FakeSchedulerService();
        var workflow = CreateWorkflow(scheduler: fakeScheduler);
        var model = new ScheduleModel { JobName = "job1", JobGroup = "DEFAULT" };

        var result = await workflow.TriggerJobNowAsync(model);

        result.IsSuccess.Should().BeTrue();
        fakeScheduler.TriggeredJobName.Should().Be("job1");
    }

    // --- Fakes ---

    private sealed class FakeSchedulerService : BlazingQuartz.Core.Services.ISchedulerService
    {
        public string? ResumedTriggerName { get; private set; }
        public string? PausedTriggerName { get; private set; }
        public string? TriggeredJobName { get; private set; }

        public Task ResumeTrigger(string triggerName, string? triggerGroup)
        {
            ResumedTriggerName = triggerName;
            return Task.CompletedTask;
        }

        public Task PauseTrigger(string triggerName, string? triggerGroup)
        {
            PausedTriggerName = triggerName;
            return Task.CompletedTask;
        }

        public Task TriggerJob(string jobName, string jobGroup)
        {
            TriggeredJobName = jobName;
            return Task.CompletedTask;
        }

        public Task<ScheduleModel> GetScheduleModelAsync(Quartz.ITrigger trigger) =>
            Task.FromResult(new ScheduleModel());

        public IAsyncEnumerable<ScheduleModel> GetAllJobsAsync(ScheduleJobFilter? filter = null) =>
            EmptyAsync<ScheduleModel>();

        public Task<JobDetailModel?> GetJobDetail(string jobName, string groupName) =>
            Task.FromResult<JobDetailModel?>(null);

        public Task CreateSchedule(JobDetailModel j, TriggerDetailModel t) => Task.CompletedTask;

        public Task<System.Collections.Generic.IReadOnlyCollection<string>> GetJobGroups() =>
            Task.FromResult<System.Collections.Generic.IReadOnlyCollection<string>>(
                System.Array.Empty<string>()
            );

        public Task<System.Collections.Generic.IReadOnlyCollection<string>> GetTriggerGroups() =>
            Task.FromResult<System.Collections.Generic.IReadOnlyCollection<string>>(
                System.Array.Empty<string>()
            );

        public Task<TriggerDetailModel?> GetTriggerDetail(string t, string g) =>
            Task.FromResult<TriggerDetailModel?>(null);

        public Task<bool> ContainsTriggerKey(string t, string g) => Task.FromResult(false);

        public Task<bool> ContainsJobKey(string j, string g) => Task.FromResult(false);

        public Task<System.Collections.Generic.IReadOnlyCollection<string>> GetCalendarNames(
            System.Threading.CancellationToken ct = default
        ) =>
            Task.FromResult<System.Collections.Generic.IReadOnlyCollection<string>>(
                System.Array.Empty<string>()
            );

        public Task<bool> DeleteSchedule(ScheduleModel m) => Task.FromResult(false);

        public Task UpdateSchedule(Key oj, Key? ot, JobDetailModel nj, TriggerDetailModel nt) =>
            Task.CompletedTask;

        public Task<Quartz.SchedulerMetaData> GetMetadataAsync() =>
            Task.FromResult<Quartz.SchedulerMetaData>(default!);

        public Task<System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<
            string,
            int
        >>> GetScheduledJobSummary() =>
            Task.FromResult<System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<
                string,
                int
            >>>(
                new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<
                    string,
                    int
                >>()
            );

        public Task PauseAllSchedules() => Task.CompletedTask;

        public Task ResumeAllSchedules() => Task.CompletedTask;

        public Task ShutdownScheduler() => Task.CompletedTask;

        public Task StartScheduler() => Task.CompletedTask;

        public Task StandbyScheduler() => Task.CompletedTask;
    }

    private sealed class FakeExecutionLogService : BlazingQuartz.Core.Services.IExecutionLogService
    {
        public Task<PagedList<ExecutionLog>> GetLatestExecutionLog(
            string jobName,
            string jobGroup,
            string? triggerName,
            string? triggerGroup,
            PageMetadata? pageMetadata = null,
            long firstLogId = 0,
            System.Collections.Generic.HashSet<LogType>? logTypes = null
        ) => Task.FromResult(new PagedList<ExecutionLog>(Array.Empty<ExecutionLog>()));

        public Task<PagedList<ExecutionLog>> GetExecutionLogs(
            ExecutionLogFilter? filter = null,
            PageMetadata? pageMetadata = null,
            long firstLogId = 0
        ) => Task.FromResult(new PagedList<ExecutionLog>(Array.Empty<ExecutionLog>()));

        public Task<System.Collections.Generic.IList<string>> GetJobNames() =>
            Task.FromResult<System.Collections.Generic.IList<string>>(System.Array.Empty<string>());

        public Task<System.Collections.Generic.IList<string>> GetJobGroups() =>
            Task.FromResult<System.Collections.Generic.IList<string>>(System.Array.Empty<string>());

        public Task<System.Collections.Generic.IList<string>> GetTriggerNames() =>
            Task.FromResult<System.Collections.Generic.IList<string>>(System.Array.Empty<string>());

        public Task<System.Collections.Generic.IList<string>> GetTriggerGroups() =>
            Task.FromResult<System.Collections.Generic.IList<string>>(System.Array.Empty<string>());

        public Task<JobExecutionStatusSummaryModel> GetJobExecutionStatusSummary(
            System.DateTimeOffset? startTimeUtc,
            System.DateTimeOffset? endTimeUtc = null
        ) => Task.FromResult(new JobExecutionStatusSummaryModel());
    }
}
