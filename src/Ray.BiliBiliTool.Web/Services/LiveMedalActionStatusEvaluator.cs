using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Web.Services;

public enum LiveMedalActionState
{
    Completed,
    Pending,
    Partial,
    WaitingLive,
    WaitingOffline,
    WaitingLighting,
    WaitingConfirmation,
    WaitingWatchTime,
    Disabled,
    Excluded,
    Unselected,
    NoWork,
    Unknown,
    DisplayOnly,
}

public sealed record LiveMedalActionStatus(
    LiveMedalActionState State,
    string Text,
    string? Reason = null
)
{
    public bool Complete => State == LiveMedalActionState.Completed;
    public string CssClass =>
        State switch
        {
            LiveMedalActionState.Completed => "medal-task-done",
            LiveMedalActionState.Pending or LiveMedalActionState.Partial => "medal-task-pending",
            LiveMedalActionState.WaitingLive
            or LiveMedalActionState.WaitingOffline
            or LiveMedalActionState.WaitingLighting
            or LiveMedalActionState.WaitingWatchTime
            or LiveMedalActionState.WaitingConfirmation => "medal-task-waiting",
            _ => "medal-task-inactive",
        };
}

public static class LiveMedalActionStatusEvaluator
{
    public static LiveMedalActionStatus Evaluate(
        LiveMedalTaskProgress task,
        LiveMedalCard medal,
        LiveFansMedalTaskOptions options,
        DateTimeOffset updatedAt,
        DateTimeOffset now,
        bool excluded = false,
        bool selected = true
    )
    {
        updatedAt = medal.ProgressUpdatedAt ?? updatedAt;
        // A platform progress snapshot is not evidence that an action is currently executing.
        if (
            medal.Error is not null
            || updatedAt.ToOffset(TimeSpan.FromHours(8)).Date
                != now.ToOffset(TimeSpan.FromHours(8)).Date
            || now - updatedAt > TimeSpan.FromMinutes(5)
            || updatedAt - now > TimeSpan.FromMinutes(1)
        )
            return new(LiveMedalActionState.Unknown, "待刷新", "更新今日任务进度后显示最新状态");

        var lighting = LiveMedalCompletionEvaluator.IsLighting(task);
        if (lighting && medal.Lighted == true)
            return new(LiveMedalActionState.Completed, "已点亮");
        if (task.Done && !lighting)
            return new(LiveMedalActionState.Completed, "已完成");
        if (excluded)
            return new(LiveMedalActionState.Excluded, "已排除", "此主播不参与自动任务");
        if (!selected)
            return new(LiveMedalActionState.Unselected, "未参与", "此主播未加入白名单");
        if (!options.IsEnable)
            return new(LiveMedalActionState.Disabled, "任务已关闭");
        if (task.Action is not ("like" or "sendDanmu" or "watchLive"))
            return new(LiveMedalActionState.DisplayOnly, "仅展示", "此动作暂未支持自动执行");
        if (!LiveMedalCompletionEvaluator.IsActionEnabled(task.Action, options))
            return new(LiveMedalActionState.Disabled, "已关闭", "此动作未开启或执行次数为零");
        if (!medal.CanInteract)
            return new(LiveMedalActionState.NoWork, "无法执行", "主播暂无可用直播间");
        if (medal.Lighted is null)
            return new(LiveMedalActionState.Unknown, "待刷新", "粉丝牌点亮状态待更新");
        if (medal.Lighted == true && medal.SavingsFull)
            return new(LiveMedalActionState.NoWork, "无需执行", "亲密度储蓄已满");
        if (lighting && task.Done)
            return new(LiveMedalActionState.WaitingConfirmation, "待确认点亮");
        if (task.Percent >= 100)
            return new(
                LiveMedalActionState.WaitingConfirmation,
                "待确认完成",
                "等待任务完成状态更新"
            );
        if (task.Action == "watchLive" && medal.Lighted == false)
            return new(
                LiveMedalActionState.WaitingLighting,
                "等待点亮",
                "点亮粉丝牌后执行观看任务"
            );
        if (
            task.Action == "watchLive"
            && !LiveFansMedalWatchScope.IsManual
            && !options.IsWatchTimeAllowed(now)
        )
            return new(
                LiveMedalActionState.WaitingWatchTime,
                "等待观看时段",
                $"每天 {options.WatchStartTime}—{options.WatchEndTime}（UTC+8）自动观看"
            );
        if (task.Action == "like" && !medal.Live)
            return new(LiveMedalActionState.WaitingLive, "待开播", "主播开播后可点赞");
        if (task.Action == "sendDanmu" && options.DanmakuOnlyWhenOffline && medal.Live)
            return new(
                LiveMedalActionState.WaitingOffline,
                "待下播",
                "已设置仅在主播未开播时发送弹幕"
            );
        if (
            task.Action == "sendDanmu"
            && !LiveMedalCompletionEvaluator.CanRun(task.Action, medal, options)
        )
            return new(
                LiveMedalActionState.WaitingLighting,
                "等待点亮",
                "直播中优先通过点赞点亮粉丝牌"
            );

        return task.Action switch
        {
            "like" => task.Percent > 0
                ? new(LiveMedalActionState.Partial, "点赞未达标")
                : new(LiveMedalActionState.Pending, "待点赞"),
            "sendDanmu" => task.Percent > 0
                ? new(LiveMedalActionState.Partial, "弹幕未达标")
                : new(LiveMedalActionState.Pending, "待发弹幕"),
            _ => task.Percent > 0
                ? new(LiveMedalActionState.Partial, "观看未达标")
                : new(LiveMedalActionState.Pending, "待观看"),
        };
    }
}
