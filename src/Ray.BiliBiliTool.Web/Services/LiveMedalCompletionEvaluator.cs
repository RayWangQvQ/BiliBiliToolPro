using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Web.Services;

public sealed record LiveMedalCompletion(TodayTaskItemState State, string Message);

public static class LiveMedalCompletionEvaluator
{
    public static LiveMedalCompletion Evaluate(
        LiveMedalSnapshot? snapshot,
        LiveFansMedalTaskOptions options,
        DateTimeOffset now
    )
    {
        var enabled = new[] { "like", "sendDanmu", "watchLive" }
            .Where(action => IsActionEnabled(action, options))
            .ToHashSet();
        if (enabled.Count == 0)
            return new(TodayTaskItemState.NoWork, "未开启点赞、弹幕或观看任务");
        if (
            snapshot is null
            || snapshot.Error is not null
            || snapshot.UpdatedAt.ToOffset(TimeSpan.FromHours(8)).Date
                != now.ToOffset(TimeSpan.FromHours(8)).Date
            || now - snapshot.UpdatedAt > TimeSpan.FromMinutes(5)
            || snapshot.UpdatedAt - now > TimeSpan.FromMinutes(1)
        )
            return new(TodayTaskItemState.Unknown, "今日粉丝牌进度暂未获取，请刷新");

        var excluded = options.GetExcludedAnchorIds();
        var included = options.GetIncludedAnchorIds();
        var medals = snapshot
            .Medals.Where(card =>
                card.CanInteract
                && !excluded.Contains(card.AnchorId)
                && (!options.OnlySelectedAnchors || included.Contains(card.AnchorId))
            )
            .ToList();
        if (medals.Count == 0)
            return new(TodayTaskItemState.NoWork, "当前没有参与任务的主播");

        var completed = 0;
        var pending = 0;
        var unlit = 0;
        var unknown = 0;
        var actionable = false;
        foreach (var medal in medals)
        {
            if (medal.Error is not null || medal.Lighted is null)
            {
                unknown++;
                continue;
            }
            var tasks = medal.Tasks.Where(task => enabled.Contains(task.Action)).ToList();
            if (medal.Tasks.Count == 0 && !(medal.Lighted == true && medal.SavingsFull))
            {
                unknown++;
                continue;
            }
            if (medal.Lighted == false)
            {
                // Lighting alternatives share one goal: the medal must actually become lit.
                var lighting = tasks.Where(task => task.Action is "like" or "sendDanmu").ToList();
                if (lighting.Count == 0)
                {
                    continue;
                }
                unlit++;
                pending++;
                actionable |= lighting.Any(task => CanRun(task.Action, medal, options, now));
            }
            else if (
                medal.SavingsFull
                || (tasks.Count > 0 && tasks.All(task => task.Done || IsLighting(task)))
            )
                completed++;
            else if (tasks.Any(task => !task.Done))
            {
                pending++;
                actionable |= tasks.Any(task =>
                    !task.Done && CanRun(task.Action, medal, options, now)
                );
            }
        }
        var total = completed + pending + unknown;
        var message = $"已完成 {completed} / {total} 个粉丝牌";
        if (unlit > 0)
            message += $" · 待点亮 {unlit} 个";
        if (unknown > 0)
            return new(TodayTaskItemState.Unknown, message + $" · {unknown} 个进度待刷新");
        if (total == 0)
            return new(TodayTaskItemState.NoWork, "当前没有已开启的粉丝牌任务");
        if (pending == 0)
            return new(TodayTaskItemState.Completed, message);
        if (
            !actionable
            && !LiveFansMedalWatchScope.IsManual
            && !options.IsWatchTimeAllowed(now)
            && medals.Any(medal =>
                medal.Lighted == true
                && medal.Tasks.Any(task => task.Action == "watchLive" && !task.Done)
            )
            && IsActionEnabled("watchLive", options)
        )
            return new(
                TodayTaskItemState.WaitingWatchTime,
                message
                    + $" · 等待观看时段 {options.WatchStartTime}—{options.WatchEndTime}（UTC+8）"
            );
        if (!actionable)
            return new(
                TodayTaskItemState.WaitingConditions,
                message + " · 等待主播状态满足任务条件"
            );
        return new(TodayTaskItemState.NotDone, message);
    }

    internal static bool IsActionEnabled(string action, LiveFansMedalTaskOptions options) =>
        action switch
        {
            "like" => options.EnableLike
                && (
                    (!options.UseLiveStateMonitoring && options.FollowDailyTaskLimit)
                    || options.GetInteractionLimit("like") > 0
                ),
            "sendDanmu" => options.EnableDanmaku
                && (
                    (!options.UseLiveStateMonitoring && options.FollowDailyTaskLimit)
                    || options.GetInteractionLimit("sendDanmu") > 0
                ),
            "watchLive" => options.EnableWatch
                && (
                    (!options.UseLiveStateMonitoring && options.FollowDailyTaskLimit)
                    || options.GetInteractionLimit("watchLive") > 0
                ),
            _ => false,
        };

    internal static bool IsLighting(LiveMedalTaskProgress task) =>
        task.Progress.Contains("仅点亮", StringComparison.Ordinal);

    internal static bool CanRun(
        string action,
        LiveMedalCard medal,
        LiveFansMedalTaskOptions options,
        DateTimeOffset? now = null
    ) =>
        action switch
        {
            "like" => medal.Live,
            "watchLive" => medal.Lighted == true
                && (
                    LiveFansMedalWatchScope.IsManual
                    || options.IsWatchTimeAllowed(now ?? DateTimeOffset.UtcNow)
                ),
            "sendDanmu" => (!options.DanmakuOnlyWhenOffline || !medal.Live)
                && !(medal.Lighted == false && medal.Live && options.EnableLike),
            _ => false,
        };
}
