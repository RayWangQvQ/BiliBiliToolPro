using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Components.Comps;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class LiveMedalActionDisplayTests : TestContext
{
    public LiveMedalActionDisplayTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static LiveMedalSnapshot Example() => LiveMedalDashboardTests.Example();

    [Fact]
    public void FreshCardsShowRemainingQuotaAndCachedCardsWaitForRefresh()
    {
        var snapshot = Example();
        using var page = RenderComponent<LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "示例账号")]).Add(x => x.Snapshot, snapshot)
        );
        Assert.Contains("还需 90 次点赞", page.Markup);
        Assert.NotEmpty(page.FindAll(".medal-task-quota"));
        page.SetParametersAndRender(p =>
            p.Add(x => x.Snapshot, snapshot with { UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1) })
        );
        Assert.Empty(page.FindAll(".medal-task-quota"));
    }

    [Fact]
    public void ActionsShowDifferentPendingPartialAndWaitingStatesDuringProgressRefresh()
    {
        var page = RenderComponent<LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "示例账号")])
                .Add(
                    x => x.Snapshot,
                    Example() with
                    {
                        Medals =
                        [
                            Example().Medals[0],
                            new(
                                33,
                                "示例未开播主播",
                                "示例牌",
                                12,
                                false,
                                false,
                                false,
                                [
                                    new("like", "点赞10次", "仅点亮", false, null),
                                    new("sendDanmu", "弹幕1次", "仅点亮", false, null),
                                    new("watchLive", "观看10分钟", "0/10", false, 0),
                                ],
                                null
                            ),
                        ],
                    }
                )
                .Add(x => x.Loading, true)
        );
        Assert.Contains("点赞未达标", Action(page, 11, "like"));
        Assert.Contains("弹幕1次", Action(page, 33, "sendDanmu"));
        Assert.Contains("待发弹幕", Action(page, 33, "sendDanmu"));
        Assert.Contains("已完成", Action(page, 11, "sendDanmu"));
        Assert.Contains("观看未达标", Action(page, 11, "watchLive"));
        Assert.Contains("等待点亮", Action(page, 33, "watchLive"));
        Assert.Contains("点亮粉丝牌后执行观看任务", Action(page, 33, "watchLive"));
        Assert.Contains("待开播", Action(page, 33, "like"));
        Assert.DoesNotContain("进行中", page.Markup);
        Assert.Equal(
            "70",
            page.Find("article[data-anchor='11'] [data-action='like'] [role='progressbar']")
                .GetAttribute("aria-valuenow")
        );
        Assert.Empty(
            page.FindAll("article[data-anchor='33'] [data-action='like'] [role='progressbar']")
        );
        page.SetParametersAndRender(p =>
            p.Add(
                x => x.TaskOptions,
                new LiveFansMedalTaskOptions
                {
                    UseLiveStateMonitoring = false,
                    FollowDailyTaskLimit = false,
                    EnableWatch = false,
                    DanmakuOnlyWhenOffline = true,
                }
            )
        );
        Assert.Contains("已关闭", Action(page, 11, "watchLive"));
        Assert.Contains("已完成", Action(page, 11, "sendDanmu"));
    }

    [Fact]
    public void LitOfflineMedalWatchingShowsPendingThenActualPlatformCompletion()
    {
        var task = new LiveMedalTaskProgress("watchLive", "观看15分钟", "每日上限 0/10", false, 0);
        var card = new LiveMedalCard(
            41,
            "示例主播",
            "示例牌",
            20,
            false,
            true,
            false,
            [task],
            null
        );
        var snapshot = new LiveMedalSnapshot([card], DateTimeOffset.UtcNow);
        var page = RenderComponent<LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "示例账号")]).Add(x => x.Snapshot, snapshot)
        );
        Assert.Contains("待观看", Action(page, 41, "watchLive"));
        Assert.DoesNotContain("待开播", Action(page, 41, "watchLive"));
        page.SetParametersAndRender(p =>
            p.Add(
                x => x.Snapshot,
                snapshot with
                {
                    Medals =
                    [
                        card with
                        {
                            Tasks =
                            [
                                task with
                                {
                                    Done = true,
                                    Percent = 100,
                                    Progress = "每日上限 10/10",
                                },
                            ],
                        },
                    ],
                }
            )
        );
        Assert.Contains("已完成", Action(page, 41, "watchLive"));
    }

    [Fact]
    public void SelectionAndTaskSwitchesExplainWhyPendingActionsWillNotRun()
    {
        var snapshot = Example();
        var page = RenderComponent<LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "示例账号")])
                .Add(x => x.Snapshot, snapshot)
                .Add(x => x.ExcludedAnchorIds, "11")
        );
        Assert.Contains("已排除", Action(page, 11, "like"));
        page.SetParametersAndRender(p =>
            p.Add(x => x.ExcludedAnchorIds, "")
                .Add(x => x.OnlySelectedAnchors, true)
                .Add(x => x.IncludedAnchorIds, "22")
        );
        Assert.Contains("未参与", Action(page, 11, "like"));
        page.SetParametersAndRender(p =>
            p.Add(x => x.OnlySelectedAnchors, false)
                .Add(
                    x => x.TaskOptions,
                    new LiveFansMedalTaskOptions
                    {
                        UseLiveStateMonitoring = false,
                        FollowDailyTaskLimit = false,
                        IsEnable = false,
                    }
                )
        );
        Assert.Contains("任务已关闭", Action(page, 11, "like"));
        Assert.Contains("已完成", Action(page, 11, "sendDanmu"));
    }

    [Fact]
    public void CachedCompletionShowsPendingRefreshUntilFreshProgressArrives()
    {
        var cached = Example() with { UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1) };
        var page = RenderComponent<LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "示例账号")])
                .Add(x => x.Snapshot, cached)
                .Add(x => x.Loading, true)
        );
        Assert.Contains("待刷新", Action(page, 11, "sendDanmu"));
        Assert.DoesNotContain("已完成", Action(page, 11, "sendDanmu"));
        Assert.Equal(
            "100",
            page.Find("article[data-anchor='11'] [data-action='sendDanmu'] [role='progressbar']")
                .GetAttribute("aria-valuenow")
        );
        page.SetParametersAndRender(p =>
            p.Add(x => x.Snapshot, cached with { UpdatedAt = DateTimeOffset.UtcNow })
                .Add(x => x.Loading, false)
        );
        Assert.Contains("已完成", Action(page, 11, "sendDanmu"));
    }

    private static string Action(
        IRenderedComponent<LiveMedalDashboard> page,
        long anchor,
        string action
    ) => page.Find($"article[data-anchor='{anchor}'] [data-action='{action}']").TextContent;
}
