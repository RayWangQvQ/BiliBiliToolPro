using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Web.Services;

/// <summary>页面上单个检查项的状态</summary>
public enum TodayTaskItemState
{
    /// <summary>❓ 状态未知（B站查询失败/Cookie失效）</summary>
    Unknown,

    /// <summary>✅ 已完成</summary>
    Completed,

    /// <summary>❌ 今天还没做</summary>
    NotDone,

    /// <summary>⚠️ 执行过但失败</summary>
    Failed,

    /// <summary>⚠️ 自动重试已达上限仍未完成</summary>
    RetryExhausted,

    /// <summary>⏳ 等待执行（今天的计划时间还没到）</summary>
    Waiting,

    /// <summary>➖ 本日无需执行</summary>
    NotToday,

    /// <summary>⛔ 已关闭</summary>
    Disabled,
}

/// <summary>B站每日任务接口的当日快照</summary>
public sealed record BiliDailyRewardSnapshot(bool Login, bool Watch, bool Share, int CoinExp);

/// <summary>判定单个检查项所需的全部输入</summary>
public sealed class TodayTaskItemContext
{
    public required TaskDefinition Task { get; init; }
    public required TaskItemDefinition Item { get; init; }

    /// <summary>任务总开关</summary>
    public required bool IsTaskEnabled { get; init; }

    /// <summary>检查项自身的开关</summary>
    public required bool IsItemEnabled { get; init; }

    /// <summary>该任务今天有没有触发点</summary>
    public required bool HasFireTimeToday { get; init; }

    /// <summary>今天最后一次触发时间是否已过（含宽限期）</summary>
    public required bool IsPastDueTime { get; init; }

    /// <summary>B站每日任务状态；仅当 Item.Source == BiliDailyReward 且查询成功时非 null</summary>
    public BiliDailyRewardSnapshot? BiliReward { get; init; }

    /// <summary>B站查询是否失败（网络异常 / Cookie 失效）</summary>
    public bool BiliQueryFailed { get; init; }

    /// <summary>今天该账号该任务的全部执行记录（含任务级与子项级，升序）</summary>
    public required IReadOnlyList<TaskRecord> Records { get; init; }

    /// <summary>今天该检查项被自动执行的次数</summary>
    public required int AutoAttempts { get; init; }

    /// <summary>自动执行次数上限</summary>
    public required int MaxAutoAttempts { get; init; }
}

/// <summary>判定结果</summary>
public sealed record TodayTaskItemResult(
    TodayTaskItemState State,
    string? Message,
    DateTimeOffset? CompletedAt,
    int AutoAttempts
);
