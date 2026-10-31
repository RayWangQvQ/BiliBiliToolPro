using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

/// <summary>
/// 稍后再看相关接口
/// </summary>
[Headers("Host: api.bilibili.com", "Origin: https://www.bilibili.com")]
public interface IWatchLaterApi
{
    /// <summary>
    /// 获取稍后再看列表
    /// </summary>
    /// <param name="ps">每页条数</param>
    /// <param name="pn">页码，从 1 开始</param>
    /// <param name="ck"></param>
    [Headers("Referer: https://www.bilibili.com/")]
    [Get("/x/v2/history/toview?ps={ps}&pn={pn}")]
    Task<BiliApiResponse<WatchLaterListResponse>> GetListAsync(
        int ps,
        int pn,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 从稍后再看中删除指定视频
    /// </summary>
    /// <param name="aid">视频 AV 号</param>
    /// <param name="viewed">"true"=按已看完删除，B 站按字符串解析，统一传小写</param>
    /// <param name="csrf">bili_jct，B 站 CSRF 校验必须</param>
    /// <param name="ck"></param>
    [Headers("Referer: https://www.bilibili.com/")]
    [Post("/x/v2/history/toview/del?aid={aid}&viewed={viewed}&csrf={csrf}")]
    Task<BiliApiResponse> DeleteAsync(
        long aid,
        string viewed,
        string csrf,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 清除所有已看完的稍后再看视频
    /// </summary>
    /// <param name="csrf">bili_jct，B 站 CSRF 校验必须</param>
    /// <param name="ck"></param>
    [Headers("Referer: https://www.bilibili.com/")]
    [Post("/x/v2/history/toview/clear?csrf={csrf}")]
    Task<BiliApiResponse> ClearViewedAsync(string csrf, [Header("Cookie")] string ck);

    /// <summary>
    /// 通过 BV 号从稍后再看中删除视频
    /// </summary>
    /// <param name="bvid">视频 BV 号</param>
    /// <param name="csrf">bili_jct，B 站 CSRF 校验必须</param>
    /// <param name="ck"></param>
    [Headers("Referer: https://www.bilibili.com/")]
    [Post("/x/v2/history/toview/bvid/del?bvid={bvid}&csrf={csrf}")]
    Task<BiliApiResponse> DeleteByBvidAsync(string bvid, string csrf, [Header("Cookie")] string ck);

    /// <summary>
    /// 获取用户历史播放记录（用于交叉查询真实观看进度）
    /// </summary>
    /// <param name="ps">每页条数，B 站上限 30</param>
    /// <param name="viewAt">翻页游标：上一页最后一条的 view_at（Unix 秒），首页传 0</param>
    /// <param name="ck"></param>
    [Headers("Referer: https://www.bilibili.com/")]
    [Get("/x/web-interface/history/cursor?ps={ps}&view_at={viewAt}")]
    Task<BiliApiResponse<HistoryCursorResponse>> GetHistoryCursorAsync(
        int ps,
        long viewAt,
        [Header("Cookie")] string ck
    );
}
