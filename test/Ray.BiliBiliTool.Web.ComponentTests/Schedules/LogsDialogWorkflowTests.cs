using FluentAssertions;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests.Schedules;

public class LogsDialogWorkflowTests
{
    private static LogsDialogWorkflow CreateWorkflow(FakeLogRepository? repo = null) =>
        new LogsDialogWorkflow(repo ?? new FakeLogRepository());

    [Fact]
    public async Task GetLatestRunInstanceIdAsync_DelegatesToRepository()
    {
        var repo = new FakeLogRepository(instanceId: "instance-42");
        var workflow = CreateWorkflow(repo);

        var result = await workflow.GetLatestRunInstanceIdAsync("job1", "trigger1");

        result.Should().Be("instance-42");
        repo.LastJobName.Should().Be("job1");
        repo.LastTriggerName.Should().Be("trigger1");
    }

    [Fact]
    public async Task GetLatestRunInstanceIdAsync_ReturnsNullWhenRepositoryReturnsNull()
    {
        var workflow = CreateWorkflow(new FakeLogRepository(instanceId: null));

        var result = await workflow.GetLatestRunInstanceIdAsync("job1", "trigger1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLogsForRunAsync_DelegatesToRepository()
    {
        var expectedLogs = new List<BiliLogs>
        {
            new BiliLogs { Timestamp = DateTime.UtcNow, Level = "INFO" },
        };
        var repo = new FakeLogRepository(logs: expectedLogs);
        var workflow = CreateWorkflow(repo);
        using var cts = new System.Threading.CancellationTokenSource();

        var result = await workflow.GetLogsForRunAsync("instance-1", 300, cts.Token);

        result.Should().BeSameAs(expectedLogs);
        repo.LastFireInstanceId.Should().Be("instance-1");
        repo.LastMaxCount.Should().Be(300);
    }

    private sealed class FakeLogRepository(string? instanceId = null, List<BiliLogs>? logs = null)
        : IExecutionLogRepository
    {
        public string? LastJobName { get; private set; }
        public string? LastTriggerName { get; private set; }
        public string? LastFireInstanceId { get; private set; }
        public int LastMaxCount { get; private set; }

        public Task<string?> GetLatestRunInstanceIdAsync(string jobName, string triggerName)
        {
            LastJobName = jobName;
            LastTriggerName = triggerName;
            return Task.FromResult(instanceId);
        }

        public Task<List<BiliLogs>> GetLogsForRunAsync(
            string fireInstanceId,
            int maxCount,
            System.Threading.CancellationToken ct
        )
        {
            LastFireInstanceId = fireInstanceId;
            LastMaxCount = maxCount;
            return Task.FromResult(logs ?? new List<BiliLogs>());
        }
    }
}
