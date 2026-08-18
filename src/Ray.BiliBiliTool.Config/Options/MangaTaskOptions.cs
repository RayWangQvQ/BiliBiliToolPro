using System.Collections.Generic;

namespace Ray.BiliBiliTool.Config.Options;

public class MangaTaskOptions : BaseConfigOptions
{
    public override string SectionName => "MangaTaskConfig";

    /// <summary>
    /// 自定义漫画阅读 comic_id（单本兼容字段，CustomComics 为空时使用；默认指向 PR #562 示例值）
    /// </summary>
    public long CustomComicId { get; set; } = 27355;

    /// <summary>
    /// 自定义漫画阅读 ep_id（单本兼容字段，CustomComics 为空时使用）
    /// </summary>
    public long CustomEpId { get; set; } = 381662;

    /// <summary>
    /// 自定义漫画阅读列表（多本时使用）。
    /// 为空时回退到 CustomComicId/CustomEpId 单本配置；不为空时按顺序循环调用 ReadManga。
    /// 每次成功调用 ReadManga 对应 B 站"今日推荐 1 本"的阅读任务进度（1→+5, 2→+10, 3→+20, 4→+20, 5→+30，共 +85 经验）。
    ///
    /// 注意：
    ///   1. B 站要求每本"再读 5 分钟"才计入；本工具仅触发 AddHistory 信号，不会真停 5 分钟刷阅读时长。
    ///      建议至少在本机上配合 App 手动停留其中一本 5 分钟以满足 B 站风控。
    ///   2. 添加配置示例（appsettings.json）：
    ///      "MangaTaskConfig": {
    ///        "CustomComics": [
    ///          { "ComicId": 27355, "EpId": 381662 },
    ///          { "ComicId": 11111, "EpId": 22222 }
    ///        ]
    ///      }
    /// </summary>
    public List<ComicReadTarget> CustomComics { get; set; } = new();

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
