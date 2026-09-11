using System.Collections.Generic;

namespace Ray.BiliBiliTool.Config.Options;

public class MangaTaskOptions : BaseConfigOptions
{
    public override string SectionName => "MangaTaskConfig";

    /// <summary>
    /// 自定义漫画阅读 comic_id（单本兼容字段，CustomComics 为空且 CustomComicId > 0 时使用）。
    /// 默认 0 表示未配置——此时若 UseSeasonBookList 开启，自动读取 B 站当日指定推荐漫画（默认 5 本）。
    /// </summary>
    public long CustomComicId { get; set; } = 0;

    /// <summary>
    /// 自定义漫画阅读 ep_id（单本兼容字段，与 CustomComicId 配对使用）
    /// </summary>
    public long CustomEpId { get; set; } = 0;

    /// <summary>
    /// 自定义漫画阅读列表（多本时使用，优先级最高）。
    /// 不为空时按顺序循环调用 ReadManga，不再自动抓取今日推荐。
    /// 添加配置示例（appsettings.json）：
    ///   "MangaTaskConfig": {
    ///     "CustomComics": [
    ///       { "ComicId": 27355, "EpId": 381662 },
    ///       { "ComicId": 11111, "EpId": 22222 }
    ///     ]
    ///   }
    /// </summary>
    public List<ComicReadTarget> CustomComics { get; set; } = new();

    /// <summary>
    /// 未配置 CustomComics 时，自动获取"今日推荐（0点更新）"每日阅读书单
    /// （user.v1.SeasonV2/GetSeasonInfo 的 data.day_task.book_task，B 站官方书单，仅需网页 Cookie），
    /// 并读取其中前 N 本。默认 5 本——对应 B 站每日阅读任务 1→+5, 2→+10, 3→+20, 4→+20, 5→+30，共 +85 经验。
    /// 设为 0 表示不自动获取（仅当显式配置了 CustomComics 时才读）。
    /// </summary>
    public int MangaReadCount { get; set; } = 5;

    /// <summary>
    /// 是否启用"自动获取今日书单（SeasonV2/GetSeasonInfo）"作为每日阅读来源。
    /// 关闭后，仅有 CustomComics / CustomComicId 配置时才读，否则跳过。
    /// 默认开启（修复 issue #1098：每日阅读必须读 B 站当天指定的书，而非任意 5 本）。
    /// </summary>
    public bool UseSeasonBookList { get; set; } = true;

    public override Dictionary<string, string> ToConfigDictionary()
    {
        var dict = new Dictionary<string, string>
        {
            { $"{SectionName}:{nameof(CustomComicId)}", CustomComicId.ToString() },
            { $"{SectionName}:{nameof(CustomEpId)}", CustomEpId.ToString() },
        };

        int i = 0;
        foreach (var c in CustomComics ?? new List<ComicReadTarget>())
        {
            if (c == null)
                continue;
            dict[$"{SectionName}:{nameof(CustomComics)}:{i}:{nameof(ComicReadTarget.ComicId)}"] =
                c.ComicId.ToString();
            dict[$"{SectionName}:{nameof(CustomComics)}:{i}:{nameof(ComicReadTarget.EpId)}"] =
                c.EpId.ToString();
            i++;
        }

        dict[$"{SectionName}:{nameof(MangaReadCount)}"] = MangaReadCount.ToString();
        dict[$"{SectionName}:{nameof(UseSeasonBookList)}"] = UseSeasonBookList.ToString();

        return MergeConfigDictionary(dict);
    }
}

/// <summary>
/// 单本漫画阅读目标（对应 B 站漫画 AddHistory 接口参数 comic_id + ep_id）
/// </summary>
public class ComicReadTarget
{
    public long ComicId { get; set; }

    public long EpId { get; set; }
}
