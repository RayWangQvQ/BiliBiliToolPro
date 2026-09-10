using BlazingQuartz.Core.Models;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Components.Pages.Schedules;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;
using Xunit;
using BzKey = BlazingQuartz.Core.Models.Key;

namespace Ray.BiliBiliTool.Web.ComponentTests.Schedules;

public class LogsDialogComponentTests : TestContext
{
    public LogsDialogComponentTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void LogsDialog_WithNullFireInstanceId_RendersWithoutException()
    {
        // Workflow returns null → dialog shows empty state without crashing
        Services.AddSingleton<ILogsDialogWorkflow>(new FakeLogsDialogWorkflow(instanceId: null));

        var cut = RenderComponent<LogsDialog>(parameters =>
            parameters
                .Add(p => p.JobKey, new BzKey("job1", "DEFAULT"))
                .Add(p => p.TriggerKey, new BzKey("trigger1", "DEFAULT"))
        );

        cut.Markup.Should().NotBeNull();
    }

    [Fact]
    public void LogsDialog_WithFireInstanceId_RendersContent()
    {
        Services.AddSingleton<ILogsDialogWorkflow>(
            new FakeLogsDialogWorkflow(instanceId: "inst-1", logs: new List<BiliLogs>())
        );

        var cut = RenderComponent<LogsDialog>(parameters =>
            parameters
                .Add(p => p.JobKey, new BzKey("job1", "DEFAULT"))
                .Add(p => p.TriggerKey, new BzKey("trigger1", "DEFAULT"))
        );

        cut.Markup.Should().NotBeNull();
    }

    private sealed class FakeLogsDialogWorkflow(
        string? instanceId = null,
        List<BiliLogs>? logs = null
    ) : ILogsDialogWorkflow
    {
        public Task<string?> GetLatestRunInstanceIdAsync(string jobName, string triggerName) =>
            Task.FromResult(instanceId);

        public Task<List<BiliLogs>> GetLogsForRunAsync(
            string fireInstanceId,
            int maxCount,
            System.Threading.CancellationToken ct
        ) => Task.FromResult(logs ?? new List<BiliLogs>());
    }
}
