using BlazingQuartz.Core.Models;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Components.Pages.Schedules;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;
using Xunit;
using BzKey = BlazingQuartz.Core.Models.Key;

namespace Ray.BiliBiliTool.Web.ComponentTests.Schedules;

public class LogsDialogTests : TestContext
{
    public LogsDialogTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void OnInitializedAsync_NullFireInstanceId_DoesNotFetchLogs()
    {
        var workflow = new FakeLogsDialogWorkflow(instanceId: null);
        Services.AddSingleton<ILogsDialogWorkflow>(workflow);

        RenderComponent<LogsDialog>(parameters =>
            parameters
                .Add(p => p.JobKey, new BzKey("job1", "DEFAULT"))
                .Add(p => p.TriggerKey, new BzKey("trigger1", "DEFAULT"))
        );

        workflow.LatestRunRequests.Should().Be(1);
        workflow.LogRequests.Should().Be(0);
    }

    [Fact]
    public async Task OnInitializedAsync_FireInstanceId_FetchesAndRendersSelectedRun()
    {
        var workflow = new FakeLogsDialogWorkflow(
            instanceId: "inst-1",
            logs:
            [
                new BiliLogs
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    RenderedMessage = "expected log entry",
                },
            ]
        );
        Services.AddSingleton<ILogsDialogWorkflow>(workflow);

        var provider = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            [nameof(LogsDialog.JobKey)] = new BzKey("job1", "DEFAULT"),
            [nameof(LogsDialog.TriggerKey)] = new BzKey("trigger1", "DEFAULT"),
        };
        await ((IServiceProvider)Services)
            .GetRequiredService<IDialogService>()
            .ShowAsync<LogsDialog>("日志", parameters);

        provider.WaitForAssertion(() =>
        {
            provider.Find(".log-level-error").TextContent.Should().Be("ERR");
            provider.Find(".log-message").TextContent.Should().Be("expected log entry");
        });
        workflow.LatestRunRequests.Should().Be(1);
        workflow.LogRequests.Should().BeGreaterThan(0);
        workflow.LastFireInstanceId.Should().Be("inst-1");
        workflow.LastMaxCount.Should().Be(300);
        provider.Dispose();
    }

    private sealed class FakeLogsDialogWorkflow(
        string? instanceId = null,
        List<BiliLogs>? logs = null
    ) : ILogsDialogWorkflow
    {
        public int LatestRunRequests { get; private set; }
        public int LogRequests { get; private set; }
        public string? LastFireInstanceId { get; private set; }
        public int LastMaxCount { get; private set; }

        public Task<string?> GetLatestRunInstanceIdAsync(string jobName, string triggerName)
        {
            LatestRunRequests++;
            return Task.FromResult(instanceId);
        }

        public Task<List<BiliLogs>> GetLogsForRunAsync(
            string fireInstanceId,
            int maxCount,
            System.Threading.CancellationToken ct
        )
        {
            LogRequests++;
            LastFireInstanceId = fireInstanceId;
            LastMaxCount = maxCount;
            return Task.FromResult(logs ?? new List<BiliLogs>());
        }
    }
}
