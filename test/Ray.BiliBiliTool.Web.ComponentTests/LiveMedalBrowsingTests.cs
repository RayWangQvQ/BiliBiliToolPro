using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using MudBlazor.State;
using Ray.BiliBiliTool.Web.Components.Comps;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public sealed class LiveMedalBrowsingTests : TestContext
{
    public LiveMedalBrowsingTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void ExclusionsSortLastEvenWhenPinnedAndPinsSortBeforeHigherLevels()
    {
        var page = RenderComponent<LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "示例账号")])
                .Add(x => x.Snapshot, Many(5))
                .Add(x => x.ExcludedAnchorIds, "1,2")
                .Add(x => x.PinnedAnchorIds, "1,5")
        );
        Assert.Equal(["5", "3", "4", "1", "2"], Anchors(page));
        page.SetParametersAndRender(p => p.Add(x => x.ExcludedAnchorIds, "2"));
        Assert.Equal(["1", "5", "3", "4", "2"], Anchors(page));
    }

    [Fact]
    public void PinTogglePreservesOtherAccountsAndDoesNotSubmitTheForm()
    {
        string? pins = null;
        var page = RenderComponent<LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "示例账号")])
                .Add(x => x.Snapshot, Many(2))
                .Add(x => x.PinnedAnchorIds, "999")
                .Add(x => x.PinnedAnchorIdsChanged, (string value) => pins = value)
        );
        var button = page.Find("article[data-anchor='2'] button.medal-pin");
        Assert.Equal("button", button.GetAttribute("type"));
        button.Click();
        Assert.Equal("2,999", pins);
        page.SetParametersAndRender(p => p.Add(x => x.PinnedAnchorIds, pins!));
        Assert.Equal("2", Anchors(page)[0]);
        Assert.Equal(
            "true",
            page.Find("article[data-anchor='2'] button").GetAttribute("aria-pressed")
        );
        page.Find("article[data-anchor='2'] button").Click();
        Assert.Equal("999", pins);
    }

    [Fact]
    public async Task PaginationSearchPageSizeAndShrinkingSnapshotKeepAValidPage()
    {
        var page = RenderComponent<LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "账号1"), new(1, "账号2")])
                .Add(x => x.Snapshot, Many(15))
        );
        Assert.Equal(6, page.FindAll("article").Count);
        var pagination = page.FindComponent<MudPagination>();
        Assert.Contains("第 1 / 3 页", page.Markup);
        await page.InvokeAsync(() => pagination.Instance.SelectedChanged.InvokeAsync(3));
        Assert.Equal(["13", "14", "15"], Anchors(page));
        page.Find("input[type=search]").Input("主播1");
        Assert.Contains("第 1 / 2 页", page.Markup);
        Assert.Equal(6, page.FindAll("article").Count);
        page.Find("input[type=search]").Input("");
        page.Find("select[aria-label='每页主播数量']").Change("12");
        Assert.Equal(12, page.FindAll("article").Count);
        await page.InvokeAsync(() =>
            page.FindComponent<MudPagination>().Instance.SelectedChanged.InvokeAsync(2)
        );
        page.SetParametersAndRender(p => p.Add(x => x.Snapshot, Many(2)));
        Assert.Equal(2, page.FindAll("article").Count);
        Assert.Contains("第 1 / 1 页", page.Markup);
        page.SetParametersAndRender(p => p.Add(x => x.Snapshot, Many(15)));
        await page.InvokeAsync(() =>
            page.FindComponent<MudPagination>().Instance.SelectedChanged.InvokeAsync(2)
        );
        page.SetParametersAndRender(p => p.Add(x => x.AccountIndex, 1));
        Assert.Equal("1", Anchors(page)[0]);
        page.Find("input[type=search]").Input("不存在");
        Assert.Empty(page.FindAll("article"));
        Assert.Contains("没有匹配", page.Markup);
    }

    [Fact]
    public async Task ManualPagingPreservesControlsPositionButRefreshAndSearchDoNotScroll()
    {
        string[]? capturedAnchors = null;
        string[]? renderedAnchors = null;
        IRenderedComponent<LiveMedalDashboard>? page = null;
        JSInterop
            .Setup<double?>(
                "biliTool.captureMedalPagePosition",
                invocation =>
                {
                    Assert.IsType<ElementReference>(Assert.Single(invocation.Arguments));
                    capturedAnchors = Anchors(page!);
                    return true;
                }
            )
            .SetResult(520d);
        JSInterop
            .SetupVoid(
                "biliTool.restoreMedalPagePosition",
                invocation =>
                {
                    Assert.IsType<ElementReference>(invocation.Arguments[0]);
                    Assert.Equal(520d, Assert.IsType<double>(invocation.Arguments[1]));
                    renderedAnchors = Anchors(page!);
                    return true;
                }
            )
            .SetVoidResult();
        page = RenderComponent<LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "示例账号")]).Add(x => x.Snapshot, Many(15))
        );
        Assert.Equal("-1", page.Find(".medal-pagination").GetAttribute("tabindex"));
        Assert.DoesNotContain(
            JSInterop.Invocations,
            x => x.Identifier == "biliTool.captureMedalPagePosition"
        );
        await page.InvokeAsync(() =>
            page.FindComponent<MudPagination>().Instance.SelectedChanged.InvokeAsync(3)
        );
        page.WaitForAssertion(() => Assert.Equal(["13", "14", "15"], renderedAnchors));
        Assert.Equal(["1", "2", "3", "4", "5", "6"], capturedAnchors);
        await page.InvokeAsync(() =>
            page.FindComponent<MudPagination>().Instance.SelectedChanged.InvokeAsync(3)
        );
        page.SetParametersAndRender(p =>
            p.Add(x => x.Snapshot, Many(14)).Add(x => x.BackgroundRefreshing, true)
        );
        page.Find("input[type=search]").Input("主播1");
        page.Find("select[aria-label='每页主播数量']").Change("12");
        Assert.Single(
            JSInterop.Invocations,
            x => x.Identifier == "biliTool.restoreMedalPagePosition"
        );
        await page.InvokeAsync(() =>
            page.FindComponent<MudPagination>().Instance.SelectedChanged.InvokeAsync(1)
        );
        Assert.Single(
            JSInterop.Invocations,
            x => x.Identifier == "biliTool.restoreMedalPagePosition"
        );
        page.Find("input[type=search]").Input("");
        await page.InvokeAsync(() =>
            page.FindComponent<MudPagination>().Instance.SelectedChanged.InvokeAsync(2)
        );
        page.WaitForAssertion(() =>
            Assert.Equal(
                2,
                JSInterop.Invocations.Count(x =>
                    x.Identifier == "biliTool.restoreMedalPagePosition"
                )
            )
        );
        Assert.Equal(["13", "14"], renderedAnchors);
        Assert.Contains(
            JSInterop.Invocations,
            x => x.Identifier == "biliTool.resetMedalPagePosition"
        );
        page.SetParametersAndRender(p => p.Add(x => x.AccountIndex, 1));
        Assert.Equal(
            2,
            JSInterop.Invocations.Count(x => x.Identifier == "biliTool.restoreMedalPagePosition")
        );
    }

    [Fact]
    public void RefreshKeepsExistingCardsVisibleAlongsideProgressAndWarning()
    {
        var page = RenderComponent<LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "示例账号")])
                .Add(
                    x => x.Snapshot,
                    Many(2) with
                    {
                        UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    }
                )
                .Add(x => x.Loading, true)
        );
        Assert.Equal(2, page.FindAll("article").Count);
        Assert.Contains("上次已完成", page.Markup);
        Assert.Contains("正在更新今日进度", page.Markup);
        page.SetParametersAndRender(p =>
            p.Add(x => x.Loading, false).Add(x => x.RefreshError, "进度更新未完成，请刷新")
        );
        Assert.Equal(2, page.FindAll("article").Count);
        Assert.Contains("进度更新未完成", page.Markup);
    }

    [Fact]
    public async Task DiskCacheSurvivesRestartIsolatesAccountsAndKeepsLastSuccessfulSnapshot()
    {
        var directory = Directory.CreateTempSubdirectory("medal-cache-");
        try
        {
            var first = new FileLiveMedalSnapshotStore(
                directory.FullName,
                NullLogger<FileLiveMedalSnapshotStore>.Instance
            );
            var key = new string('A', 64);
            var yesterday = Many(2) with { UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1) };
            await first.WriteAsync(key, yesterday);
            var restarted = new FileLiveMedalSnapshotStore(
                directory.FullName,
                NullLogger<FileLiveMedalSnapshotStore>.Instance
            );
            var restored = await restarted.ReadAsync(key);
            Assert.Equal(yesterday.UpdatedAt, restored!.UpdatedAt);
            Assert.Equal([1L, 2L], restored.Medals.Select(card => card.AnchorId));
            Assert.Null(await restarted.ReadAsync(new string('B', 64)));
            await restarted.WriteAsync(key, new([], DateTimeOffset.UtcNow, "读取失败"));
            Assert.Equal(yesterday.UpdatedAt, (await restarted.ReadAsync(key))!.UpdatedAt);
            await restarted.WriteAsync(
                key,
                new([yesterday.Medals[0] with { Error = "部分读取失败" }], DateTimeOffset.UtcNow)
            );
            var partial = Assert.Single((await restarted.ReadAsync(key))!.Medals);
            Assert.Equal("部分读取失败", partial.Error);
            Assert.False(partial.Tasks[0].Done);
            Assert.Single(Directory.GetFiles(directory.FullName));
            await Assert.ThrowsAsync<ArgumentException>(() => restarted.ReadAsync("../invalid"));
            await first.WriteAsync(
                key,
                yesterday with
                {
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-8),
                }
            );
            Assert.Null(await restarted.ReadAsync(key));
            await File.WriteAllTextAsync(
                Path.Combine(directory.FullName, key + ".json"),
                "invalid json"
            );
            Assert.Null(await restarted.ReadAsync(key));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static string[] Anchors(IRenderedComponent<LiveMedalDashboard> page) =>
        page.FindAll("article").Select(card => card.GetAttribute("data-anchor")!).ToArray();

    internal static LiveMedalSnapshot Many(int count) =>
        new(
            Enumerable
                .Range(1, count)
                .Select(index => new LiveMedalCard(
                    index,
                    $"主播{index}",
                    $"示例牌{index}",
                    30 - index,
                    false,
                    true,
                    false,
                    [new("like", "每日点赞", "5/10", false, 50)],
                    null
                ))
                .ToList(),
            DateTimeOffset.UtcNow
        );
}
