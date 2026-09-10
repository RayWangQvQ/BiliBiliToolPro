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

public class HistoryDialogComponentTests : TestContext
{
    public HistoryDialogComponentTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void HistoryDialog_OnInitialized_RendersWithoutException()
    {
        Services.AddSingleton<IHistoryDialogWorkflow>(new FakeHistoryDialogWorkflow());

        var cut = RenderComponent<HistoryDialog>(parameters =>
            parameters
                .Add(p => p.JobKey, new BzKey("job1", "DEFAULT"))
                .Add(p => p.TriggerKey, new BzKey("trigger1", "DEFAULT"))
        );

        cut.Markup.Should().NotBeNull();
    }

    [Fact]
    public void HistoryDialog_WithEmptyHistory_DoesNotShowLoadMoreButton()
    {
        // Empty page → HasMore = false → no "Load More" button
        var emptyPage = new PagedList<ExecutionLog>(Array.Empty<ExecutionLog>());
        Services.AddSingleton<IHistoryDialogWorkflow>(new FakeHistoryDialogWorkflow(emptyPage));

        var cut = RenderComponent<HistoryDialog>(parameters =>
            parameters
                .Add(p => p.JobKey, new BzKey("job1", "DEFAULT"))
                .Add(p => p.TriggerKey, new BzKey("trigger1", "DEFAULT"))
        );

        // No "Load More" button visible when page is empty
        cut.FindAll("button")
            .Should()
            .NotContain(b =>
                b.TextContent.Contains("Load More", System.StringComparison.OrdinalIgnoreCase)
            );
    }

    private sealed class FakeHistoryDialogWorkflow(PagedList<ExecutionLog>? result = null)
        : IHistoryDialogWorkflow
    {
        public Task<PagedList<ExecutionLog>> GetHistoryPageAsync(
            string jobName,
            string jobGroup,
            string? triggerName,
            string? triggerGroup,
            PageMetadata pageMetadata,
            long firstLogId
        ) => Task.FromResult(result ?? new PagedList<ExecutionLog>(Array.Empty<ExecutionLog>()));
    }
}
