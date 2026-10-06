using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class LiveMedalActionStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 20, 30, 0, TimeSpan.FromHours(8));

    [Theory]
    [InlineData("pending", "like", LiveMedalActionState.Pending, "待点赞")]
    [InlineData("pending", "sendDanmu", LiveMedalActionState.Pending, "待发弹幕")]
    [InlineData("pending", "watchLive", LiveMedalActionState.Pending, "待观看")]
    [InlineData("partial", "like", LiveMedalActionState.Partial, "点赞未达标")]
    [InlineData("partial", "sendDanmu", LiveMedalActionState.Partial, "弹幕未达标")]
    [InlineData("partial", "watchLive", LiveMedalActionState.Partial, "观看未达标")]
    [InlineData("offline", "like", LiveMedalActionState.WaitingLive, "待开播")]
    [InlineData("offline", "watchLive", LiveMedalActionState.Pending, "待观看")]
    [InlineData("offline", "sendDanmu", LiveMedalActionState.Pending, "待发弹幕")]
    [InlineData("offlineOnly", "sendDanmu", LiveMedalActionState.WaitingOffline, "待下播")]
    [InlineData("lighting", "sendDanmu", LiveMedalActionState.WaitingLighting, "等待点亮")]
    [InlineData("lighting", "like", LiveMedalActionState.Pending, "待点赞")]
    [InlineData("lighting", "watchLive", LiveMedalActionState.WaitingLighting, "等待点亮")]
    [InlineData("alreadyLit", "sendDanmu", LiveMedalActionState.Completed, "已点亮")]
    [InlineData("lightingDone", "like", LiveMedalActionState.WaitingConfirmation, "待确认点亮")]
    [InlineData("done", "watchLive", LiveMedalActionState.Completed, "已完成")]
    [InlineData("doneButOfflineAndDisabled", "like", LiveMedalActionState.Completed, "已完成")]
    [InlineData("full", "watchLive", LiveMedalActionState.NoWork, "无需执行")]
    [InlineData("fullButUnlit", "like", LiveMedalActionState.Pending, "待点赞")]
    [InlineData("zero", "like", LiveMedalActionState.Disabled, "已关闭")]
    [InlineData("zero", "sendDanmu", LiveMedalActionState.Disabled, "已关闭")]
    [InlineData("zero", "watchLive", LiveMedalActionState.Disabled, "已关闭")]
    [InlineData("disabled", "like", LiveMedalActionState.Disabled, "已关闭")]
    [InlineData("disabled", "sendDanmu", LiveMedalActionState.Disabled, "已关闭")]
    [InlineData("disabled", "watchLive", LiveMedalActionState.Disabled, "已关闭")]
    [InlineData("taskDisabled", "like", LiveMedalActionState.Disabled, "任务已关闭")]
    [InlineData("excluded", "like", LiveMedalActionState.Excluded, "已排除")]
    [InlineData("unselected", "like", LiveMedalActionState.Unselected, "未参与")]
    [InlineData("noRoom", "like", LiveMedalActionState.NoWork, "无法执行")]
    [InlineData("unconfirmed", "like", LiveMedalActionState.WaitingConfirmation, "待确认完成")]
    [InlineData("pending", "gift", LiveMedalActionState.DisplayOnly, "仅展示")]
    public void StatusFollowsActionProgressAndExecutionConditions(
        string scenario,
        string action,
        LiveMedalActionState expected,
        string text
    )
    {
        var options = new LiveFansMedalTaskOptions
        {
            UseLiveStateMonitoring = false,
            FollowDailyTaskLimit = false,
        };
        var task = new LiveMedalTaskProgress(action, "示例动作", "0/10", false, 0);
        var medal = new LiveMedalCard(1, "示例主播", "示例牌", 12, true, true, false, [task], null);
        switch (scenario)
        {
            case "partial":
                task = task with { Progress = "3/10", Percent = 30 };
                break;
            case "offline":
                medal = medal with { Live = false };
                break;
            case "offlineOnly":
                options.DanmakuOnlyWhenOffline = true;
                break;
            case "lighting":
                medal = medal with { Lighted = false };
                task = task with { Progress = "仅点亮", Percent = null };
                break;
            case "alreadyLit":
                task = task with { Progress = "仅点亮", Percent = null };
                break;
            case "lightingDone":
                medal = medal with { Lighted = false };
                task = task with { Progress = "仅点亮", Done = true, Percent = 100 };
                break;
            case "done":
                task = task with { Done = true, Percent = 100 };
                break;
            case "doneButOfflineAndDisabled":
                task = task with { Done = true, Percent = 100 };
                medal = medal with { Live = false };
                options.IsEnable = false;
                break;
            case "full":
                medal = medal with { SavingsFull = true };
                break;
            case "fullButUnlit":
                medal = medal with { SavingsFull = true, Lighted = false };
                task = task with { Progress = "仅点亮", Percent = null };
                break;
            case "zero":
                options.LikeNumber = options.SendDanmakuNumber = options.HeartBeatNumber = 0;
                break;
            case "disabled":
                options.EnableLike = options.EnableDanmaku = options.EnableWatch = false;
                break;
            case "taskDisabled":
                options.IsEnable = false;
                break;
            case "noRoom":
                medal = medal with { CanInteract = false };
                break;
            case "unconfirmed":
                task = task with { Progress = "10/10", Percent = 100 };
                break;
        }
        var result = LiveMedalActionStatusEvaluator.Evaluate(
            task,
            medal,
            options,
            Now,
            Now,
            scenario == "excluded",
            scenario != "unselected"
        );
        Assert.Equal(expected, result.State);
        Assert.Equal(text, result.Text);
        Assert.Equal(expected == LiveMedalActionState.Completed, result.Complete);
        Assert.DoesNotContain("进行中", result.Text);
    }

    [Theory]
    [InlineData("yesterday")]
    [InlineData("stale")]
    [InlineData("future")]
    [InlineData("readError")]
    [InlineData("lightingUnknown")]
    public void UnavailableOrStaleDataNeverShowsCurrentCompletion(string scenario)
    {
        var task = new LiveMedalTaskProgress("like", "点赞", "10/10", true, 100);
        var medal = new LiveMedalCard(1, "示例主播", "示例牌", 12, true, true, false, [task], null);
        var updated = scenario switch
        {
            "yesterday" => Now.AddDays(-1),
            "stale" => Now.AddMinutes(-6),
            "future" => Now.AddMinutes(2),
            _ => Now,
        };
        if (scenario == "readError")
            medal = medal with { Error = "暂未读取" };
        if (scenario == "lightingUnknown")
        {
            medal = medal with { Lighted = null };
            task = task with { Done = false };
        }
        var result = LiveMedalActionStatusEvaluator.Evaluate(task, medal, new(), updated, Now);
        Assert.Equal(LiveMedalActionState.Unknown, result.State);
        Assert.Equal("待刷新", result.Text);
        Assert.False(result.Complete);
    }

    [Theory]
    [InlineData("like", "待点赞")]
    [InlineData("sendDanmu", "待发弹幕")]
    [InlineData("watchLive", "待观看")]
    public void FollowDailyGoalsEnablesActionsWithoutCustomBudgets(string action, string text)
    {
        var task = new LiveMedalTaskProgress(action, "示例动作", "0/10", false, 0);
        var medal = new LiveMedalCard(1, "示例主播", "示例牌", 12, true, true, false, [task], null);
        var result = LiveMedalActionStatusEvaluator.Evaluate(
            task,
            medal,
            new()
            {
                UseLiveStateMonitoring = false,
                FollowDailyTaskLimit = true,
                LikeNumber = 0,
                SendDanmakuNumber = 0,
                HeartBeatNumber = 0,
            },
            Now,
            Now
        );
        Assert.Equal(LiveMedalActionState.Pending, result.State);
        Assert.Equal(text, result.Text);
    }
}
