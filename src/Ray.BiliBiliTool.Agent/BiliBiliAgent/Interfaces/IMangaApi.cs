using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.MangaApi;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

/// <summary>
/// 漫画相关接口
/// </summary>
[Headers("Origin: https://manga.bilibili.com", "Host: manga.bilibili.com")]
public interface IMangaApi
{
    /// <summary>
    /// 漫画签到
    /// </summary>
    /// <param name="platform"></param>
    /// <returns></returns>
    [Post("/twirp/activity.v1.Activity/ClockIn?platform={platform}")]
    Task<BiliApiResponse> ClockIn(string platform, [Header("Cookie")] string ck);

    /// <summary>
    /// 漫画分享（每日分享 +5 积分，实测仅需网页 Cookie 即可调用，返回 data.point）
    /// </summary>
    [Post("/twirp/activity.v1.Activity/ShareComic?platform={platform}")]
    Task<BiliApiResponse> ShareComic(string platform, [Header("Cookie")] string ck);

    /// <summary>
    /// 漫画阅读
    /// </summary>
    /// <param name="platform"></param>
    /// <returns></returns>
    [Post(
        "/twirp/bookshelf.v1.Bookshelf/AddHistory?platform={platform}&comic_id={comic_id}&ep_id={ep_id}"
    )]
    Task<BiliApiResponse> ReadManga(
        string platform,
        long comic_id,
        long ep_id,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 获取会员漫画奖励
    /// </summary>
    /// <param name="reason_id"></param>
    /// <returns></returns>
    [Post("/twirp/user.v1.User/GetVipReward?reason_id={reason_id}")]
    Task<BiliApiResponse<MangaVipRewardResponse>> ReceiveMangaVipReward(
        int reason_id,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 获取赛季活动信息（含每日阅读任务书单）。
    /// 实测：POST user.v1.SeasonV2/GetSeasonInfo，body {"type":1}，
    /// 仅需网页 Cookie 鉴权（无签名），每天 0 点更新的"今日推荐"5 本书就在
    /// data.day_task.book_task 里（每本含 comic_id id / read_min / user_read_min / point）。
    /// 这是"每日阅读 +5/+10/+20/+20/+30 经验"任务的官方书单来源。
    /// </summary>
    [Post("/twirp/user.v1.SeasonV2/GetSeasonInfo")]
    Task<BiliApiResponse<SeasonInfoResponse>> GetSeasonInfo(
        [Body] SeasonRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 上报漫画阅读时长（App 端"读满 5 分钟"的心跳上报）。
    /// 实测 path 因版本而异（旧版 activity.v1.Activity/CompleteReadTask，
    /// 新版路径可能变化），若 404 需从抓包确认；不影响 SeasonV2 书单+AddHistory 主流程。
    /// </summary>
    [Post("/twirp/activity.v1.Activity/CompleteReadTask")]
    Task<BiliApiResponse> CompleteReadTask([Query] int read_minute, [Header("Cookie")] string ck);
}
