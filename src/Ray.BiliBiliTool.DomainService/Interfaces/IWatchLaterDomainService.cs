using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces;

/// <summary>
/// 稍后再看清理
/// </summary>
public interface IWatchLaterDomainService : IDomainService
{
    /// <summary>
    /// 获取稍后再看视频列表（自动分页获取全部）
    /// </summary>
    /// <param name="ck"></param>
    /// <param name="pageSize">每页条数，接口上限 50</param>
    /// <param name="cancellationToken"></param>
    Task<List<WatchLaterItem>> GetAllItemsAsync(
        BiliCookie ck,
        int pageSize = 50,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 删除单个稍后再看视频
    /// </summary>
    Task<bool> DeleteAsync(
        long aid,
        BiliCookie ck,
        bool viewed = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 通过 BV 号删除稍后再看视频
    /// </summary>
    Task<bool> DeleteByBvidAsync(
        string bvid,
        BiliCookie ck,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 一键清空所有已看完的稍后再看视频
    /// </summary>
    Task<int> ClearAllViewedAsync(BiliCookie ck, CancellationToken cancellationToken = default);

    /// <summary>
    /// 备份稍后再看列表到 JSON 文件，返回备份文件路径
    /// </summary>
    /// <param name="items">要备份的列表</param>
    /// <param name="mid">用户 mid（用于文件名区分多账号）</param>
    /// <param name="directory">备份目录</param>
    /// <param name="extraMeta">额外元数据（任务名、阈值等）</param>
    Task<string?> BackupToFileAsync(
        List<WatchLaterItem> items,
        string mid,
        string directory = "backup",
        Dictionary<string, object>? extraMeta = null
    );

    /// <summary>
    /// 拉取用户历史播放记录，返回 bvid -> HistoryItem 的 Map
    /// </summary>
    /// <param name="maxPages">最多拉几页（每页 30 条），默认 10 页 = 300 条</param>
    Task<Dictionary<string, HistoryItem>> GetHistoryMapAsync(
        BiliCookie ck,
        int maxPages = 10,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 通过交叉历史记录，从稍后再看列表中识别"已看完"（在历史中 progress >= duration * threshold）的视频
    /// </summary>
    /// <param name="items">稍后再看列表</param>
    /// <param name="historyMap">历史记录 Map（bvid -> HistoryItem）</param>
    /// <param name="threshold">看完阈值（0.0~1.0），默认 0.95</param>
    List<WatchLaterItem> FindViewedInHistory(
        List<WatchLaterItem> items,
        Dictionary<string, HistoryItem> historyMap,
        double threshold = 0.95
    );

    /// <summary>
    /// 清理过期的备份文件
    /// </summary>
    /// <param name="directory">备份目录</param>
    /// <param name="retentionDays">保留天数，0 = 永久保留</param>
    Task<int> CleanExpiredBackupsAsync(string directory, int retentionDays);

    /// <summary>
    /// 清理已看完的视频（分页拉取 + 交叉历史 + 逐条删除），返回删除条数
    /// </summary>
    Task<int> CleanViewedAsync(
        BiliCookie ck,
        int pageSize = 50,
        int intervalMs = 1000,
        bool alsoDeleteInvalid = true,
        double viewedThreshold = 0.95,
        bool useHistoryCrossCheck = true,
        int historyMaxPages = 10,
        bool backupBeforeClean = true,
        string backupDirectory = "backup",
        int backupRetentionDays = 7,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 清空整个稍后再看列表（不论是否已看完）
    /// </summary>
    Task<int> CleanAllAsync(
        BiliCookie ck,
        int pageSize = 50,
        int intervalMs = 1000,
        bool backupBeforeClean = true,
        string backupDirectory = "backup",
        int backupRetentionDays = 7,
        CancellationToken cancellationToken = default
    );
}
