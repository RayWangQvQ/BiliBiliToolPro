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
}
