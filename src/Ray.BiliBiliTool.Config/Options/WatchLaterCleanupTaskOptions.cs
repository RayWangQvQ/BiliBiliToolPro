namespace Ray.BiliBiliTool.Config.Options;

/// <summary>
/// 稍后再看清理任务配置
/// </summary>
public class WatchLaterCleanupTaskOptions : BaseConfigOptions
{
    public override string SectionName => "WatchLaterCleanupTaskConfig";

    /// <summary>
    /// 是否只清理已看完的视频
    /// 为 true 时，仅清理已看完或失效的视频；
    /// 为 false 时，清空整个稍后再看列表（默认 true）
    /// </summary>
    public bool OnlyViewed { get; set; } = true;

    /// <summary>
    /// 单次请求的页大小（默认 50）
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// 是否同时删除失效视频（被删除/不可播放的），默认 true
    /// </summary>
    public bool DeleteInvalid { get; set; } = true;

    /// <summary>
    /// 删除单条之间的间隔（毫秒），默认 1000ms
    /// </summary>
    public int DeleteIntervalMs { get; set; } = 1000;

    /// <summary>
    /// 是否在清理前备份稍后再看列表到 JSON 文件
    /// 默认 true，备份到 ./backup/watchlater_{mid}_{yyyyMMdd_HHmmss}.json
    /// </summary>
    public bool BackupBeforeClean { get; set; } = true;

    /// <summary>
    /// 备份文件保留天数，超过此天数的备份将被自动删除
    /// 默认 7 天，设为 0 表示永久保留
    /// </summary>
    public int BackupRetentionDays { get; set; } = 7;

    /// <summary>
    /// 备份文件目录（相对于程序运行目录）
    /// 默认 ./backup
    /// </summary>
    public string BackupDirectory { get; set; } = "backup";

    /// <summary>
    /// 已看完判定阈值（0.0 ~ 1.0），默认 0.95
    /// 即历史记录中 progress / duration >= 该值 时认为已看完。
    /// 0.95 对齐 B 站官方"看完 95% 算已看完"的标准。
    /// </summary>
    public double ViewedThreshold { get; set; } = 0.95;

    /// <summary>
    /// 是否用历史记录交叉检查已看完状态（默认 true）
    /// B 站稍后再看 API 返回的 progress 字段不可信（已看完的视频会被重置为 -1），
    /// 必须用 /x/web-interface/history/cursor 拉真实历史进度再交叉对比。
    /// 关掉此选项将仅用 toview.progress 判断（基本识别不到已看完）。
    /// </summary>
    public bool UseHistoryCrossCheck { get; set; } = true;

    /// <summary>
    /// 历史记录最多拉几页（每页 30 条），默认 10 页 = 300 条最近历史
    /// 0 表示拉所有（可能很慢，建议保持 10）
    /// </summary>
    public int HistoryMaxPages { get; set; } = 10;

    public override Dictionary<string, string> ToConfigDictionary()
    {
        return MergeConfigDictionary(
            new Dictionary<string, string>
            {
                { $"{SectionName}:{nameof(OnlyViewed)}", OnlyViewed.ToString().ToLower() },
                { $"{SectionName}:{nameof(PageSize)}", PageSize.ToString() },
                { $"{SectionName}:{nameof(DeleteInvalid)}", DeleteInvalid.ToString().ToLower() },
                { $"{SectionName}:{nameof(DeleteIntervalMs)}", DeleteIntervalMs.ToString() },
                {
                    $"{SectionName}:{nameof(BackupBeforeClean)}",
                    BackupBeforeClean.ToString().ToLower()
                },
                { $"{SectionName}:{nameof(BackupRetentionDays)}", BackupRetentionDays.ToString() },
                { $"{SectionName}:{nameof(BackupDirectory)}", BackupDirectory },
                { $"{SectionName}:{nameof(ViewedThreshold)}", ViewedThreshold.ToString("F2") },
                {
                    $"{SectionName}:{nameof(UseHistoryCrossCheck)}",
                    UseHistoryCrossCheck.ToString().ToLower()
                },
                { $"{SectionName}:{nameof(HistoryMaxPages)}", HistoryMaxPages.ToString() },
            }
        );
    }
}
