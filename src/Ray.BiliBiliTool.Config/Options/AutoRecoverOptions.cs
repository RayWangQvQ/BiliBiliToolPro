namespace Ray.BiliBiliTool.Config.Options;

/// <summary>
/// 自动补做配置。用固定间隔触发（Quartz SimpleTrigger），因此没有 Cron。
/// </summary>
public class AutoRecoverOptions
{
    public const string SectionName = "AutoRecoverConfig";

    /// <summary>是否启用自动补做</summary>
    public bool IsEnable { get; set; } = true;

    /// <summary>每隔几小时检查一次 [1,24]</summary>
    public int IntervalHours { get; set; } = 2;

    /// <summary>执行记录保留天数 [1,90]</summary>
    public int RecordRetentionDays { get; set; } = 3;
}
