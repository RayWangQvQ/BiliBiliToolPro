using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Web.Services;

/// <summary>
/// 单个检查项的状态判定。纯逻辑、无 IO。
/// 判定优先级（自上而下，先命中先返回）：
/// 已关闭 → 本日无需执行 → 等待执行 → 状态未知 → 已完成 → 分享特例 → 已达重试上限 → 失败 → 未执行
/// </summary>
public static class TaskStatusEvaluator
{
    public static TodayTaskItemResult Evaluate(TodayTaskItemContext ctx)
    {
        if (!ctx.IsTaskEnabled || !ctx.IsItemEnabled)
        {
            return new(TodayTaskItemState.Disabled, null, null, ctx.AutoAttempts);
        }

        if (!ctx.HasFireTimeToday)
        {
            return new(TodayTaskItemState.NotToday, null, null, ctx.AutoAttempts);
        }

        if (!ctx.IsPastDueTime)
        {
            return new(TodayTaskItemState.Waiting, null, null, ctx.AutoAttempts);
        }

        var completedAt = FindCompletedAt(ctx);

        if (ctx.BiliQueryFailed)
        {
            return new(
                TodayTaskItemState.Unknown,
                "B站接口查询失败，无法判断完成情况",
                completedAt,
                ctx.AutoAttempts
            );
        }

        if (IsCompleted(ctx))
        {
            return new(TodayTaskItemState.Completed, null, completedAt, ctx.AutoAttempts);
        }

        // 分享特例：B站对该接口恒返回 -403「账号异常,操作失败」（已实测连续 13 天 100% 失败）。
        // 工具明明执行过、B站却不认，重试多少次都没用，因此不消耗自动重试次数。
        if (
            ctx.Item.ItemKey == TaskCatalog.ShareItemKey
            && ctx.Records.Any(r => r.Status == TaskRecordStatus.Success)
        )
        {
            return new(
                TodayTaskItemState.Failed,
                "B站拒绝（账号异常），无法完成",
                completedAt,
                ctx.AutoAttempts
            );
        }

        if (ctx.AutoAttempts >= ctx.MaxAutoAttempts)
        {
            // 不再附 Message：页面上的 StateText 已经写了「已自动重试 N 次仍未完成」，
            // 再给一条同义 Message 会渲染成重复文案。
            return new(TodayTaskItemState.RetryExhausted, null, completedAt, ctx.AutoAttempts);
        }

        var latest = ctx.Records.LastOrDefault();
        if (latest is { Status: TaskRecordStatus.Failed })
        {
            return new(
                TodayTaskItemState.Failed,
                latest.Message ?? "执行失败",
                completedAt,
                ctx.AutoAttempts
            );
        }

        // 今天尝试过（有记录）但 B 站仍未确认 → 视为失败，但允许继续自动重试
        if (ctx.Item.Source == TaskItemSource.BiliDailyReward && ctx.Records.Count > 0)
        {
            return new(
                TodayTaskItemState.Failed,
                "执行过但 B 站未确认完成",
                completedAt,
                ctx.AutoAttempts
            );
        }

        return new(TodayTaskItemState.NotDone, null, completedAt, ctx.AutoAttempts);
    }

    /// <summary>
    /// 是否允许「自动补做」。
    /// 除了状态本身要是未执行/失败之外，分享必须排除：B 站对该接口恒返回 -403（已实测连续 13 天
    /// 100% 失败），自动重试纯属浪费，因此只允许手动补做。
    /// </summary>
    public static bool CanAutoRedo(TodayTaskItemContext ctx, TodayTaskItemResult result) =>
        result.State is TodayTaskItemState.NotDone or TodayTaskItemState.Failed
        && ctx.Item.ItemKey != TaskCatalog.ShareItemKey;

    private static bool IsCompleted(TodayTaskItemContext ctx) =>
        ctx.Item.Source switch
        {
            TaskItemSource.BiliDailyReward => IsBiliCompleted(ctx),
            _ => ctx.Records.Any(r => r.Status == TaskRecordStatus.Success),
        };

    private static bool IsBiliCompleted(TodayTaskItemContext ctx)
    {
        if (ctx.BiliReward is null)
        {
            return false;
        }

        return ctx.Item.ItemKey switch
        {
            "Login" => ctx.BiliReward.Login,
            "Watch" => ctx.BiliReward.Watch,
            TaskCatalog.ShareItemKey => ctx.BiliReward.Share,
            "DonateCoin" => ctx.BiliReward.CoinExp > 0,
            _ => false,
        };
    }

    private static DateTimeOffset? FindCompletedAt(TodayTaskItemContext ctx)
    {
        if (ctx.Item.Source == TaskItemSource.BiliDailyReward)
        {
            return IsBiliCompleted(ctx) ? ctx.Records.LastOrDefault()?.CreatedAtUtc : null;
        }

        return ctx
            .Records.Where(r => r.Status == TaskRecordStatus.Success)
            .Select(r => (DateTimeOffset?)r.CreatedAtUtc)
            .LastOrDefault();
    }
}
