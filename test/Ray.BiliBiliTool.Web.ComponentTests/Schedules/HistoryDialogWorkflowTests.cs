using BlazingQuartz.Core.Models;
using BlazingQuartz.Core.Services;
using FluentAssertions;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests.Schedules;

public class HistoryDialogWorkflowTests
{
    private static HistoryDialogWorkflow CreateWorkflow(FakeLogService? logService = null) =>
        new HistoryDialogWorkflow(logService ?? new FakeLogService());

    [Fact]
    public async Task GetHistoryPageAsync_DelegatesToExecutionLogService()
    {
        var expectedPage = new PagedList<ExecutionLog>(Array.Empty<ExecutionLog>());
        var fake = new FakeLogService(expectedPage);
        var workflow = CreateWorkflow(fake);
        var pageMeta = new PageMetadata(0, 5);

        var result = await workflow.GetHistoryPageAsync(
            "job1",
            "DEFAULT",
            "trigger1",
            "DEFAULT",
            pageMeta,
            0L
        );

        result.Should().BeSameAs(expectedPage);
        fake.LastJobName.Should().Be("job1");
        fake.LastTriggerName.Should().Be("trigger1");
        fake.LastPageMetadata.Should().Be(pageMeta);
        fake.LastFirstLogId.Should().Be(0L);
    }

    private sealed class FakeLogService(PagedList<ExecutionLog>? result = null)
        : BlazingQuartz.Core.Services.IExecutionLogService
    {
        public string? LastJobName { get; private set; }
        public string? LastTriggerName { get; private set; }
        public PageMetadata? LastPageMetadata { get; private set; }
        public long LastFirstLogId { get; private set; }

        public Task<PagedList<ExecutionLog>> GetLatestExecutionLog(
            string jobName,
            string jobGroup,
            string? triggerName,
            string? triggerGroup,
            PageMetadata? pageMetadata = null,
            long firstLogId = 0,
            System.Collections.Generic.HashSet<LogType>? logTypes = null
        )
        {
            LastJobName = jobName;
            LastTriggerName = triggerName;
            LastPageMetadata = pageMetadata;
            LastFirstLogId = firstLogId;
            return Task.FromResult(
                result ?? new PagedList<ExecutionLog>(Array.Empty<ExecutionLog>())
            );
        }

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
