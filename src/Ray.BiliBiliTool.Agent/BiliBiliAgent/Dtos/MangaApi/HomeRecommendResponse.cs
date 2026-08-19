using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.MangaApi;

/// <summary>
/// 漫画首页推荐响应（comic.v1.Comic/HomeRecommend）。
/// 用于修复 issue #1098：每日阅读必须读 B 站当天指定的推荐漫画。
/// 实测仅需网页 Cookie 鉴权（platform=web），无需 app 签名 / WBI。
/// </summary>
public class HomeRecommendResponse
{
    /// <summary>
    /// 当日推荐漫画列表
    /// </summary>
    [JsonPropertyName("list")]
    public List<ComicRecommendItem>? List { get; set; }
}

/// <summary>
/// 单条推荐漫画
/// </summary>
public class ComicRecommendItem
{
    /// <summary>
    /// 漫画 id（B 站返回为数字）
    /// </summary>
    [JsonPropertyName("comic_id")]
    public long ComicId { get; set; }

    /// <summary>
    /// 漫画标题
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// 跳转值，形如 bilicomic://reader_progress/&lt;comic_id&gt;?aid=...&amp;cid=&lt;ep_id&gt;&amp;cover=...
    /// 其中的 cid 即为本章 ep_id（部分本此处为 0 或缺失，需回退 pv_info.cid）
    /// </summary>
    [JsonPropertyName("jump_value")]
    public string? JumpValue { get; set; }

    /// <summary>
    /// 播放/阅读信息，cid 为章节 id（B 站返回为字符串，如 "184840173"）
    /// </summary>
    [JsonPropertyName("pv_info")]
    public PvInfo? PvInfo { get; set; }
}

/// <summary>
/// 播放/阅读信息
/// </summary>
public class PvInfo
{
    /// <summary>
    /// 章节 id（字符串，需解析为 long 后作为 AddHistory 的 ep_id）
    /// </summary>
    [JsonPropertyName("cid")]
    public string? Cid { get; set; }
}

/// <summary>
/// HomeRecommend 请求体（仅需 pageNum，缺省会报 invalid_argument）
/// </summary>
public class HomeRecommendRequest
{
    [JsonPropertyName("pageNum")]
    public int PageNum { get; set; } = 1;
}
