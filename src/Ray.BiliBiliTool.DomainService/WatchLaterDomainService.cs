using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.DomainService;

/// <summary>
/// 稍后再看清理
/// </summary>
public class WatchLaterDomainService(
    ILogger<WatchLaterDomainService> logger,
    IWatchLaterApi watchLaterApi
) : IWatchLaterDomainService
{
    private const int MaxItemsPerRun = 1000;
    private const int MaxBackupFileBytes = 50 * 1024 * 1024; // 50 MB 备份上限

    /// <summary>
    /// 获取稍后再看视频列表（自动分页获取全部）
    /// </summary>
    public async Task<List<WatchLaterItem>> GetAllItemsAsync(
        BiliCookie ck,
        int pageSize = 50,
        CancellationToken cancellationToken = default
    )
    {
        var allItems = new List<WatchLaterItem>();
        int pn = 1;
        int ps = Math.Clamp(pageSize, 1, 50); //接口上限 50
        bool hasMore = true;

        while (hasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await watchLaterApi.GetListAsync(ps, pn, ck.ToString());
            if (response.Code != 0)
            {
                logger.LogWarning("获取稍后再看列表失败 (页 {pn}): {msg}", pn, response.Message);
                break;
            }

            var data = response.Data;
            if (data?.List == null || data.List.Count == 0)
                break;

            allItems.AddRange(data.List);
            //接口不返回 has_more，用总数判断是否还有下一页
            hasMore = pn * ps < data.Count;
            pn++;

            if (allItems.Count >= MaxItemsPerRun)
            {
                logger.LogWarning("稍后再看列表超过 {max} 条，已截断", MaxItemsPerRun);
                break;
            }
        }

        return allItems;
    }

    /// <summary>
    /// 删除单个稍后再看视频
    /// </summary>
    public async Task<bool> DeleteAsync(
        long aid,
        BiliCookie ck,
        bool viewed = true,
        CancellationToken cancellationToken = default
    )
    {
        var response = await watchLaterApi.DeleteAsync(
            aid,
            viewed ? "true" : "false",
            ck.BiliJct,
            ck.ToString()
        );
        if (response.Code == 0)
        {
            logger.LogInformation("【删除成功】aid={aid}", aid);
            return true;
        }

        logger.LogWarning(
            "【删除失败】aid={aid}, code={code}, msg={msg}",
            aid,
            response.Code,
            response.Message
        );
        return false;
    }

    /// <summary>
    /// 通过 BV 号删除稍后再看视频
    /// </summary>
    public async Task<bool> DeleteByBvidAsync(
        string bvid,
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        var response = await watchLaterApi.DeleteByBvidAsync(bvid, ck.BiliJct, ck.ToString());
        if (response.Code == 0)
        {
            logger.LogInformation("【删除成功】bvid={bvid}", bvid);
            return true;
        }

        logger.LogWarning(
            "【删除失败】bvid={bvid}, code={code}, msg={msg}",
            bvid,
            response.Code,
            response.Message
        );
        return false;
    }

    /// <summary>
    /// 一键清空所有已看完的稍后再看视频
    /// </summary>
    public async Task<int> ClearAllViewedAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        var response = await watchLaterApi.ClearViewedAsync(ck.BiliJct, ck.ToString());
        if (response.Code == 0)
        {
            logger.LogInformation("【一键清除已看完】成功");
            return 1;
        }

        logger.LogWarning("【一键清除已看完】失败：{msg}", response.Message);
        return 0;
    }

    /// <summary>
    /// 备份稍后再看列表到 JSON 文件
    /// </summary>
    public async Task<string?> BackupToFileAsync(
        List<WatchLaterItem> items,
        string mid,
        string directory = "backup",
        Dictionary<string, object>? extraMeta = null
    )
    {
        if (items == null || items.Count == 0)
        {
            logger.LogInformation("【备份】列表为空，跳过备份");
            return null;
        }

        try
        {
            // 确保目录存在
            var dir = Path.IsPathRooted(directory)
                ? directory
                : Path.Combine(AppContext.BaseDirectory, directory);
            Directory.CreateDirectory(dir);

            // 文件名: watchlater_{mid}_{yyyyMMdd_HHmmss}.json
            var safeMid = string.IsNullOrWhiteSpace(mid) ? "unknown" : mid;
            var fileName = $"watchlater_{safeMid}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(dir, fileName);

            var payload = new
            {
                backupAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                mid = safeMid,
                itemCount = items.Count,
                meta = extraMeta ?? new Dictionary<string, object>(),
                items,
            };

            var json = JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(
                        System.Text.Unicode.UnicodeRanges.All
                    ),
                }
            );

            if (Encoding.UTF8.GetByteCount(json) > MaxBackupFileBytes)
            {
                logger.LogWarning(
                    "【备份】文件大小超过 {max} MB 阈值，跳过写入（不写超大备份）",
                    MaxBackupFileBytes / 1024 / 1024
                );
                return null;
            }

            await File.WriteAllTextAsync(filePath, json);

            var fileInfo = new FileInfo(filePath);
            logger.LogInformation(
                "【备份成功】路径={path}, 大小={size:F2} KB, 共 {count} 条",
                filePath,
                fileInfo.Length / 1024.0,
                items.Count
            );

            return filePath;
        }
        catch (Exception ex)
        {
            // 备份失败不应阻塞清理主流程
            logger.LogWarning(ex, "【备份失败】不影响主清理任务，错误：{msg}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 清理过期的备份文件
    /// </summary>
    public Task<int> CleanExpiredBackupsAsync(string directory, int retentionDays)
    {
        if (retentionDays <= 0)
            return Task.FromResult(0);

        try
        {
            var dir = Path.IsPathRooted(directory)
                ? directory
                : Path.Combine(AppContext.BaseDirectory, directory);

            if (!Directory.Exists(dir))
                return Task.FromResult(0);

            var cutoff = DateTime.Now.AddDays(-retentionDays);
            int deleted = 0;
            foreach (
                var file in Directory.EnumerateFiles(
                    dir,
                    "watchlater_*.json",
                    SearchOption.TopDirectoryOnly
                )
            )
            {
                try
                {
                    var fi = new FileInfo(file);
                    if (fi.LastWriteTime < cutoff)
                    {
                        fi.Delete();
                        deleted++;
                        logger.LogInformation("【清理过期备份】{file}", file);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning("【清理过期备份】删除失败：{file}, {msg}", file, ex.Message);
                }
            }

            if (deleted > 0)
                logger.LogInformation("【清理过期备份】共删除 {n} 个", deleted);
            return Task.FromResult(deleted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "【清理过期备份】目录扫描失败：{msg}", ex.Message);
            return Task.FromResult(0);
        }
    }

    /// <summary>
    /// 拉取用户历史播放记录，返回 bvid -> HistoryItem 的 Map
    /// </summary>
    /// <remarks>
    /// B 站稍后再看 API 返回的 progress 字段不可信（已看完的视频会被重置为 -1）。
    /// 必须用 /x/web-interface/history/cursor 拉真实历史进度，再交叉对比。
    /// </remarks>
    public async Task<Dictionary<string, HistoryItem>> GetHistoryMapAsync(
        BiliCookie ck,
        int maxPages = 10,
        CancellationToken cancellationToken = default
    )
    {
        var result = new Dictionary<string, HistoryItem>();
        const int ps = 30; // B 站历史接口 ps 上限是 30
        long viewAt = 0;

        for (int i = 0; i < maxPages; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var response = await watchLaterApi.GetHistoryCursorAsync(ps, viewAt, ck.ToString());
            if (response.Code != 0)
            {
                logger.LogWarning("【历史记录】拉取失败 (页 {i}): {msg}", i + 1, response.Message);
                break;
            }

            var data = response.Data;
            if (data?.List == null || data.List.Count == 0)
                break;

            foreach (var item in data.List)
            {
                if (item?.History == null || string.IsNullOrEmpty(item.History.Bvid))
                    continue;

                // 同 bvid 多个分 P 时只保留进度最高的那条
                if (
                    !result.TryGetValue(item.History.Bvid, out var existing)
                    || item.Progress > existing.Progress
                )
                {
                    result[item.History.Bvid] = item;
                }
            }

            // 翻页：使用 cursor.viewAt（不是 max，max 是 oid 形式的 kid）
            viewAt = data.Cursor?.ViewAt ?? 0;
            if (viewAt <= 0)
                break;
        }

        logger.LogInformation(
            "【历史记录】共拉取 {pages} 页，{count} 个唯一 bvid",
            maxPages,
            result.Count
        );
        return result;
    }

    /// <summary>
    /// 通过交叉历史记录，从稍后再看列表中识别"已看完"的视频
    /// </summary>
    public List<WatchLaterItem> FindViewedInHistory(
        List<WatchLaterItem> items,
        Dictionary<string, HistoryItem> historyMap,
        double threshold = 0.95
    )
    {
        var viewed = new List<WatchLaterItem>();
        if (historyMap == null || historyMap.Count == 0)
            return viewed;

        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.Bvid))
                continue;

            if (!historyMap.TryGetValue(item.Bvid, out var hist))
                continue;

            // 历史中 progress >= duration * threshold 视为已看完
            if (hist.Duration > 0 && hist.Progress >= hist.Duration * threshold)
            {
                viewed.Add(item);
            }
        }

        return viewed;
    }

    /// <summary>
    /// 清理已看完的视频（分页拉取 + 交叉历史 + 逐条删除），返回删除条数
    /// </summary>
    public async Task<int> CleanViewedAsync(
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
    )
    {
        logger.LogInformation(
            "【开始清理】模式: 已看完(阈值 {th:P0}){invalid}{cross}",
            viewedThreshold,
            alsoDeleteInvalid ? " + 失效视频" : "",
            useHistoryCrossCheck ? " + 交叉历史" : ""
        );

        var items = await GetAllItemsAsync(ck, pageSize, cancellationToken);
        if (items.Count == 0)
        {
            logger.LogInformation("【结果】稍后再看列表为空，无需清理");
            return 0;
        }

        logger.LogInformation("【当前稍后再看】共 {count} 个视频", items.Count);

        // 策略 A: 用 toview.progress >= duration 判"已看完"（B 站基本不会返回真实值）
        // 策略 B: 交叉历史记录（用历史接口的 progress 字段判断）
        var toDelete = new List<WatchLaterItem>();
        var viewedByToview = items.Where(i => i.IsViewedByThreshold(viewedThreshold)).ToList();
        toDelete.AddRange(viewedByToview);

        var viewedByHistory = new List<WatchLaterItem>();
        if (useHistoryCrossCheck)
        {
            var historyMap = await GetHistoryMapAsync(ck, historyMaxPages, cancellationToken);
            viewedByHistory = FindViewedInHistory(items, historyMap, viewedThreshold);
            foreach (var v in viewedByHistory)
            {
                if (!toDelete.Any(x => x.Aid == v.Aid))
                    toDelete.Add(v);
            }
        }

        // 策略 C: 失效视频
        var invalid = new List<WatchLaterItem>();
        if (alsoDeleteInvalid)
        {
            invalid = items.Where(i => !i.IsValid).ToList();
            foreach (var v in invalid)
            {
                if (!toDelete.Any(x => x.Aid == v.Aid))
                    toDelete.Add(v);
            }
        }

        logger.LogInformation(
            "【待删除】toview已看完 {vt} 个，历史交叉已看完 {vh} 个，失效 {vi} 个，合计 {t} 个",
            viewedByToview.Count,
            viewedByHistory.Count,
            invalid.Count,
            toDelete.Count
        );

        if (toDelete.Count == 0)
        {
            logger.LogInformation("【结果】没有需要清理的视频");
            return 0;
        }

        // 备份整个列表
        if (backupBeforeClean)
        {
            await BackupToFileAsync(
                items,
                ck.UserId ?? "unknown",
                backupDirectory,
                new Dictionary<string, object>
                {
                    ["task"] = "WatchLaterCleanup",
                    ["mode"] = "viewed",
                    ["viewedThreshold"] = viewedThreshold,
                    ["useHistoryCrossCheck"] = useHistoryCrossCheck,
                    ["alsoDeleteInvalid"] = alsoDeleteInvalid,
                    ["toDeleteCount"] = toDelete.Count,
                }
            );
            await CleanExpiredBackupsAsync(backupDirectory, backupRetentionDays);
        }

        int success = 0;
        int fail = 0;
        int index = 0;
        foreach (var item in toDelete)
        {
            index++;
            if (cancellationToken.IsCancellationRequested)
                break;

            string tag;
            if (viewedByToview.Any(x => x.Aid == item.Aid))
                tag = "toview已看完";
            else if (viewedByHistory.Any(x => x.Aid == item.Aid))
                tag = "历史已看完";
            else
                tag = "失效";

            logger.LogInformation(
                "【{tag} {idx}/{total}】《{title}》aid={aid} bvid={bvid}",
                tag,
                index,
                toDelete.Count,
                Truncate(item.Title, 30),
                item.Aid,
                item.Bvid
            );

            if (await DeleteAsync(item.Aid, ck, viewed: true, cancellationToken))
                success++;
            else
                fail++;

            if (index < toDelete.Count && intervalMs > 0)
                await Task.Delay(intervalMs, cancellationToken);
        }

        logger.LogInformation("【清理完成】成功 {s} 个，失败 {f} 个", success, fail);
        return success;
    }

    /// <summary>
    /// 清空整个稍后再看列表（不论是否已看完）
    /// </summary>
    public async Task<int> CleanAllAsync(
        BiliCookie ck,
        int pageSize = 50,
        int intervalMs = 1000,
        bool backupBeforeClean = true,
        string backupDirectory = "backup",
        int backupRetentionDays = 7,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("【开始清理】模式: 清空整个稍后再看列表");

        var items = await GetAllItemsAsync(ck, pageSize, cancellationToken);
        if (items.Count == 0)
        {
            logger.LogInformation("【结果】稍后再看列表为空");
            return 0;
        }

        logger.LogInformation("【当前稍后再看】共 {count} 个视频", items.Count);

        if (backupBeforeClean)
        {
            await BackupToFileAsync(
                items,
                ck.UserId ?? "unknown",
                backupDirectory,
                new Dictionary<string, object>
                {
                    ["task"] = "WatchLaterCleanup",
                    ["mode"] = "all",
                    ["toDeleteCount"] = items.Count,
                }
            );
            await CleanExpiredBackupsAsync(backupDirectory, backupRetentionDays);
        }

        int success = 0;
        int fail = 0;
        int index = 0;
        foreach (var item in items)
        {
            index++;
            if (cancellationToken.IsCancellationRequested)
                break;

            logger.LogInformation(
                "【删除 {idx}/{total}】《{title}》aid={aid}",
                index,
                items.Count,
                Truncate(item.Title, 30),
                item.Aid
            );

            if (await DeleteAsync(item.Aid, ck, viewed: true, cancellationToken))
                success++;
            else
                fail++;

            if (index < items.Count && intervalMs > 0)
                await Task.Delay(intervalMs, cancellationToken);
        }

        logger.LogInformation("【清理完成】成功 {s} 个，失败 {f} 个", success, fail);
        return success;
    }

    private static string Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s))
            return "(无标题)";
        return s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}
