using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Components.Pages.Today;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class TodayMedalDisplayTests : TestContext
{
    [Theory]
    [InlineData(
        TodayTaskItemState.NotDone,
        "未完成",
        "已完成 0 / 5 个粉丝牌 · 待点亮 5 个",
        "0 / 1"
    )]
    [InlineData(
        TodayTaskItemState.WaitingConditions,
        "等待任务条件",
        "已完成 0 / 5 个粉丝牌 · 等待主播状态满足任务条件",
        "0 / 1"
    )]
    [InlineData(TodayTaskItemState.Completed, "已完成", "已完成 5 / 5 个粉丝牌", "1 / 1")]
    [InlineData(TodayTaskItemState.NoWork, "当前无需执行", "当前没有参与任务的主播", "0 / 0")]
    public void ProgressIsVisibleWithoutHoverAndNoWorkIsExcludedFromAccountProgress(
        TodayTaskItemState state,
        string text,
        string summary,
        string total
    )
    {
        Services.AddMudServices();
        Services.Configure<AutoRecoverOptions>(_ => { });
        JSInterop.Mode = JSRuntimeMode.Loose;
        var fake = DispatchProxy.Create<ITodayTaskService, TodayProxy>();
        ((TodayProxy)fake).Accounts =
        [
            new()
            {
                UserId = 1001,
                UserName = "示例账号",
                Index = 0,
                IsCookieValid = true,
                Groups =
                [
                    new()
                    {
                        TaskKey = "LiveFansMedalAppService",
                        DisplayName = "直播粉丝勋章",
                        Items =
                        [
                            new()
                            {
                                DisplayName = "直播粉丝勋章",
                                State = state,
                                StateText = text,
                                Message = summary,
                                ProgressSummary = summary,
                            },
                        ],
                    },
                ],
            },
        ];
        Services.AddSingleton(fake);
        var page = RenderComponent<Today>();
        Assert.Equal(summary, page.Find(".today-medal-progress").TextContent);
        Assert.Contains(text, page.Find(".today-task-row").TextContent);
        Assert.Contains("已完成 " + total, page.Markup);
        Assert.DoesNotContain("04:10", page.Markup);
        Assert.Equal(
            state == TodayTaskItemState.NotDone ? 1 : 0,
            page.FindAll(".today-task-row button").Count
        );
    }

    [Fact]
    public void DashboardCompletionCountUsesOnlyParticipatingEnabledGoals()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var snapshot = new LiveMedalSnapshot(
            [
                new(
                    1,
                    "示例主播甲",
                    "示例牌",
                    12,
                    false,
                    true,
                    false,
                    [new("like", "点赞", "仅点亮", false, null)],
                    null
                ),
                new(
                    2,
                    "示例主播乙",
                    "示例牌",
                    12,
                    false,
                    true,
                    false,
                    [new("like", "点赞", "1/1", true, 100)],
                    null
                ),
                new(
                    3,
                    "示例主播丙",
                    "示例牌",
                    12,
                    false,
                    false,
                    false,
                    [new("sendDanmu", "弹幕", "仅点亮", false, null)],
                    null
                ),
            ],
            DateTimeOffset.UtcNow
        );
        var page = RenderComponent<Ray.BiliBiliTool.Web.Components.Comps.LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "示例账号")])
                .Add(x => x.Snapshot, snapshot)
                .Add(x => x.ExcludedAnchorIds, "2")
        );
        Assert.Equal("1", page.FindAll(".medal-overview strong")[2].TextContent);
    }

    public class TodayProxy : DispatchProxy
    {
        public List<AccountTodayTasksDto> Accounts { get; set; } = [];

        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            method!.Name == "GetTodayStatusAsync"
                ? Task.FromResult(Accounts)
                : throw new InvalidOperationException(method.Name);
    }
}
