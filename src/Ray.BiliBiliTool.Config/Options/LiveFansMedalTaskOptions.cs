using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Ray.BiliBiliTool.Config.Options;

public class LiveFansMedalTaskOptions : BaseConfigOptions, IValidatableObject
{
    public override string SectionName => "LiveFansMedalTaskConfig";

    public bool UseLiveStateMonitoring { get; set; } = true;

    [Range(1, 30, ErrorMessage = "检查间隔应为 1～30 分钟")]
    public int MonitorIntervalMinutes { get; set; } = 5;
    public bool FollowDailyTaskLimit { get; set; } = true;

    [Range(0, 5000, ErrorMessage = "每日点赞次数应为 0～5000")]
    public int DailyLikeNumber { get; set; } = 300;

    [Range(0, 100, ErrorMessage = "每日弹幕次数应为 0～100")]
    public int DailyDanmakuNumber { get; set; } = 10;

    [Range(0, 1440, ErrorMessage = "每日观看时长应为 0～1440 分钟")]
    public int DailyWatchMinutes { get; set; } = 150;

    public bool UseWatchTimeWindow { get; set; }

    [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "请选择观看开始时间")]
    public string WatchStartTime { get; set; } = "08:00";

    [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "请选择观看结束时间")]
    public string WatchEndTime { get; set; } = "23:00";

    public bool IsWatchTimeAllowed(DateTimeOffset now)
    {
        if (!UseWatchTimeWindow)
            return true;
        if (!TryWatchWindow(out var start, out var end))
            return false;
        var time = now.ToOffset(TimeSpan.FromHours(8)).TimeOfDay;
        return start < end ? time >= start && time < end : time >= start || time < end;
    }

    public DateTimeOffset? NextWatchWindowBoundary(DateTimeOffset now)
    {
        if (!UseWatchTimeWindow || !TryWatchWindow(out var start, out var end))
            return null;
        var local = now.ToOffset(TimeSpan.FromHours(8));
        var boundary = new DateTimeOffset(
            local.Date + (IsWatchTimeAllowed(now) ? end : start),
            local.Offset
        );
        return boundary <= local ? boundary.AddDays(1) : boundary;
    }

    private bool TryWatchWindow(out TimeSpan start, out TimeSpan end)
    {
        start = end = TimeSpan.Zero;
        if (
            !TimeOnly.TryParseExact(
                WatchStartTime,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var first
            )
            || !TimeOnly.TryParseExact(
                WatchEndTime,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var last
            )
        )
            return false;
        start = first.ToTimeSpan();
        end = last.ToTimeSpan();
        return start != end;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UseWatchTimeWindow && !TryWatchWindow(out _, out _))
            yield return new ValidationResult(
                "开始与结束时间需不同，全天执行可关闭时段限制",
                [nameof(WatchEndTime)]
            );
    }

    public int GetInteractionLimit(string action) =>
        action switch
        {
            "like" => Math.Clamp(UseLiveStateMonitoring ? DailyLikeNumber : LikeNumber, 0, 5000),
            "sendDanmu" => Math.Clamp(
                UseLiveStateMonitoring ? DailyDanmakuNumber : SendDanmakuNumber,
                0,
                100
            ),
            "watchLive" => Math.Clamp(
                UseLiveStateMonitoring ? DailyWatchMinutes : HeartBeatNumber,
                0,
                1440
            ) * 60,
            _ => 0,
        };

    public bool EnableLike { get; set; } = true;
    public bool EnableDanmaku { get; set; } = true;
    public bool EnableWatch { get; set; } = true;
    public bool DanmakuOnlyWhenOffline { get; set; }
    public bool OnlySelectedAnchors { get; set; }
    public string IncludedAnchorIds { get; set; } = "";

    public HashSet<long> GetIncludedAnchorIds() => ParseAnchorIds(IncludedAnchorIds);

    public string ExcludedAnchorIds { get; set; } = "";

    public HashSet<long> GetExcludedAnchorIds() => ParseAnchorIds(ExcludedAnchorIds);

    public string PinnedAnchorIds { get; set; } = "";

    public HashSet<long> GetPinnedAnchorIds() => ParseAnchorIds(PinnedAnchorIds);

    private static HashSet<long> ParseAnchorIds(string value) =>
        value
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
                    property.CanWrite
                    && property.DeclaringType == typeof(LiveFansMedalTaskOptions)
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
