using System.ComponentModel.DataAnnotations;

namespace Ray.BiliBiliTool.Config.Options;

public class LiveFansMedalTaskOptions : BaseConfigOptions
{
    public override string SectionName => "LiveFansMedalTaskConfig";

    // Existing installations keep their custom budgets until this is enabled.
    public bool FollowDailyTaskLimit { get; set; }
    public bool EnableLike { get; set; } = true;
    public bool EnableDanmaku { get; set; } = true;
    public bool EnableWatch { get; set; } = true;
    public bool DanmakuOnlyWhenOffline { get; set; }
    public string ExcludedAnchorIds { get; set; } = "";

    public HashSet<long> GetExcludedAnchorIds() =>
        ExcludedAnchorIds
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => long.TryParse(value, out var id) && id > 0 ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();

    [Required(ErrorMessage = "请输入弹幕内容")]
    [StringLength(30, ErrorMessage = "弹幕内容最多 30 个字符")]
    public string DanmakuContent { get; set; } = "OvO";

    [Range(0, 1440, ErrorMessage = "观看时长应为 0～1440 分钟")]
    public int HeartBeatNumber { get; set; } = 70;

    [Range(1, 10)]
    public int HeartBeatSendGiveUpThreshold { get; set; } = 5;

    public const int HeartBeatInterval = 60;

    [Range(0, 5000, ErrorMessage = "点赞次数应为 0～5000")]
    public int LikeNumber { get; set; } = 30;

    [Range(0, 100, ErrorMessage = "弹幕次数应为 0～100")]
    public int SendDanmakuNumber { get; set; } = 1;

    [Range(1, 10)]
    public int SendDanmakugiveUpThreshold { get; set; } = 3;

    public override Dictionary<string, string> ToConfigDictionary()
    {
        var values = new Dictionary<string, string>();
        foreach (
            var property in typeof(LiveFansMedalTaskOptions)
                .GetProperties()
                .Where(property =>
                    property.DeclaringType == typeof(LiveFansMedalTaskOptions)
                    && property.Name != nameof(SectionName)
                )
        )
        {
            var value = property.GetValue(this);
            values[$"{SectionName}:{property.Name}"] = value is bool flag
                ? flag.ToString().ToLowerInvariant()
                : value?.ToString() ?? "";
        }
        return MergeConfigDictionary(values);
    }
}
