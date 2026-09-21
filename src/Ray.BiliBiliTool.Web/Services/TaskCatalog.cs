using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Web.Services;

/// <summary>检查项的状态来源</summary>
public enum TaskItemSource
{
    /// <summary>B站每日任务接口（/x/member/web/exp/reward）</summary>
    BiliDailyReward,

    /// <summary>本工具的执行记录表</summary>
    ExecutionRecord,
}

/// <summary>一个检查项（页面上的一行）。ItemKey 为 null 表示「整任务算一项」。</summary>
public sealed class TaskItemDefinition(
    string? itemKey,
    string displayName,
    TaskItemSource source,
    Func<IConfiguration, bool> isEnabled
)
{
    public string? ItemKey { get; } = itemKey;
    public string DisplayName { get; } = displayName;
    public TaskItemSource Source { get; } = source;

    /// <summary>该项是否开启（任务总开关由 TaskDefinition.IsEnabled 另行判断）</summary>
    public Func<IConfiguration, bool> IsEnabled { get; } = isEnabled;
}

/// <summary>一个任务（页面上的一组）</summary>
public sealed class TaskDefinition(
    string taskKey,
    string jobName,
    string displayName,
    Func<IConfiguration, bool> isEnabled,
    IReadOnlyList<TaskItemDefinition> items
)
{
    /// <summary>执行记录里的 TaskKey（AppService 类型名）</summary>
    public string TaskKey { get; } = taskKey;

    /// <summary>Quartz 里的 Job 名，用于读取 Cron</summary>
    public string JobName { get; } = jobName;

    public string DisplayName { get; } = displayName;
    public Func<IConfiguration, bool> IsEnabled { get; } = isEnabled;
    public IReadOnlyList<TaskItemDefinition> Items { get; } = items;
}

/// <summary>
/// 页面上展示的任务与检查项目录。这里的顺序就是页面上的显示顺序。
/// </summary>
public static class TaskCatalog
{
    /// <summary>分享子项键（命中 B 站风控特例：该接口恒返回 -403，不参与自动补做）</summary>
    public const string ShareItemKey = "Share";

    public static IReadOnlyList<TaskDefinition> All { get; } =
    [
        new TaskDefinition(
            taskKey: "DailyTaskAppService",
            jobName: "DailyJob",
            displayName: "每日任务",
            isEnabled: c => c.GetValue("DailyTaskConfig:IsEnable", true),
            items:
            [
                new TaskItemDefinition(
                    "Login",
                    "登录",
                    TaskItemSource.BiliDailyReward,
                    c => c.GetValue("DailyTaskConfig:IsEnable", true)
                ),
                new TaskItemDefinition(
                    "Watch",
                    "观看视频",
                    TaskItemSource.BiliDailyReward,
                    c => c.GetValue("DailyTaskConfig:IsWatchVideo", true)
                ),
                new TaskItemDefinition(
                    ShareItemKey,
                    "分享视频",
                    TaskItemSource.BiliDailyReward,
                    c => c.GetValue("DailyTaskConfig:IsShareVideo", true)
                ),
                new TaskItemDefinition(
                    "DonateCoin",
                    "投币",
                    TaskItemSource.BiliDailyReward,
                    c => c.GetValue("DailyTaskConfig:NumberOfCoins", 5) > 0
                ),
                new TaskItemDefinition(
                    "VipPrivilege",
                    "大会员福利",
                    TaskItemSource.ExecutionRecord,
                    c => c.GetValue("DailyTaskConfig:IsEnable", true)
                ),
            ]
        ),
        TaskLevel(
            "LiveFansMedalAppService",
            "LiveFansMedalJob",
            "直播粉丝勋章",
            "LiveFansMedalTaskConfig"
        ),
        TaskLevel("MangaTaskAppService", "MangaJob", "漫画签到/阅读", "MangaTaskConfig"),
        TaskLevel(
            "MangaPrivilegeTaskAppService",
            "MangaPrivilegeJob",
            "漫画特权",
            "MangaPrivilegeTaskConfig"
        ),
        TaskLevel(
            "Silver2CoinTaskAppService",
            "Silver2CoinJob",
            "银瓜子换硬币",
            "Silver2CoinTaskConfig"
        ),
        TaskLevel(
            "LiveLotteryTaskAppService",
            "LiveLotteryJob",
            "直播抽奖",
            "LiveLotteryTaskConfig"
        ),
        TaskLevel("ChargeTaskAppService", "ChargeJob", "充电", "ChargeTaskConfig"),
        TaskLevel("VipBigPointAppService", "VipBigPointJob", "大会员积分", "VipBigPointConfig"),
        TaskLevel(
            "UnfollowBatchedTaskAppService",
            "UnfollowBatchedJob",
            "批量取关",
            "UnfollowBatchedTaskConfig"
        ),
    ];

    /// <summary>整任务作为一个检查项（ItemKey 为 null）</summary>
    private static TaskDefinition TaskLevel(
        string taskKey,
        string jobName,
        string displayName,
        string configSection
    )
    {
        Func<IConfiguration, bool> enabled = c => c.GetValue($"{configSection}:IsEnable", true);

        return new TaskDefinition(
            taskKey,
            jobName,
            displayName,
            enabled,
            [new TaskItemDefinition(null, displayName, TaskItemSource.ExecutionRecord, enabled)]
        );
    }
}
