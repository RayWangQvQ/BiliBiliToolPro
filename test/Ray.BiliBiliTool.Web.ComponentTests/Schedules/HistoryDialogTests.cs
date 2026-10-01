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

public class HistoryDialogTests : TestContext
{
    public HistoryDialogTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task OnInitializedAsync_ValidKeys_RequestsFirstPageForSelectedJob()
    {
        var workflow = new FakeHistoryDialogWorkflow();
        Services.AddSingleton<IHistoryDialogWorkflow>(workflow);

        var provider = await ShowHistoryDialogAsync();

        provider.Find(".mud-dialog").TextContent.Should().Contain("执行历史");
        workflow.LastJobName.Should().Be("job1");
        workflow.LastTriggerName.Should().Be("trigger1");
        workflow.LastPageMetadata.Should().Be(new PageMetadata(0, 5));
        workflow.LastFirstLogId.Should().Be(0);
    }

    [Fact]
    public async Task OnInitializedAsync_EmptyHistory_DoesNotShowLoadMoreButton()
    {
        var emptyPage = new PagedList<ExecutionLog>(Array.Empty<ExecutionLog>());
        Services.AddSingleton<IHistoryDialogWorkflow>(new FakeHistoryDialogWorkflow(emptyPage));

        var provider = await ShowHistoryDialogAsync();

        provider
            .FindAll("button")
            .Should()
            .NotContain(b => b.TextContent.Contains("加载更多", System.StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetMoreLogs_FullFirstPage_UsesNextPageAndFirstLogId()
    {
        var firstPage = new PagedList<ExecutionLog>(
            Enumerable.Range(10, 5).Select(id => new ExecutionLog { LogId = id }),
            new PageMetadata(0, 5)
        );
        var workflow = new FakeHistoryDialogWorkflow(firstPage);
        Services.AddSingleton<IHistoryDialogWorkflow>(workflow);
        var provider = await ShowHistoryDialogAsync();

        var loadMore = provider
            .FindAll("button")
            .Single(button => button.TextContent.Contains("加载更多", StringComparison.Ordinal));
        loadMore.Click();

        provider.WaitForAssertion(() => workflow.PageRequests.Should().Equal(0, 1));
        workflow.LastFirstLogId.Should().Be(10);
        provider
            .FindAll("button")
            .Should()
            .NotContain(button =>
                button.TextContent.Contains("加载更多", StringComparison.Ordinal)
            );
    }

    private async Task<IRenderedComponent<MudDialogProvider>> ShowHistoryDialogAsync()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            [nameof(HistoryDialog.JobKey)] = new BzKey("job1", "DEFAULT"),
            [nameof(HistoryDialog.TriggerKey)] = new BzKey("trigger1", "DEFAULT"),
        };
        await ((IServiceProvider)Services)
            .GetRequiredService<IDialogService>()
            .ShowAsync<HistoryDialog>("执行历史", parameters);
        provider.WaitForAssertion(() => provider.Find(".mud-dialog"));
        return provider;
    }

    private sealed class FakeHistoryDialogWorkflow(PagedList<ExecutionLog>? result = null)
        : IHistoryDialogWorkflow
    {
        public string? LastJobName { get; private set; }
        public string? LastTriggerName { get; private set; }
        public PageMetadata? LastPageMetadata { get; private set; }
        public long LastFirstLogId { get; private set; }
        public List<int> PageRequests { get; } = new();

        public Task<PagedList<ExecutionLog>> GetHistoryPageAsync(
            string jobName,
            string jobGroup,
            string? triggerName,
            string? triggerGroup,
            PageMetadata pageMetadata,
            long firstLogId
        )
        {
            LastJobName = jobName;
            LastTriggerName = triggerName;
            LastPageMetadata = pageMetadata;
            LastFirstLogId = firstLogId;
            PageRequests.Add(pageMetadata.Page);
            return Task.FromResult(
                pageMetadata.Page == 0 && result is not null
                    ? result
                    : new PagedList<ExecutionLog>(Array.Empty<ExecutionLog>())
            );
        }
    }
}
