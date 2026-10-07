using System.ComponentModel;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Article;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Charge;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Coin;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.UpInfo;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint.ThreeDaysSign;
using Refit;
using UpInfoDto = Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.UpInfo.UpInfo;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

/// <summary>
/// api.bilibili.com 的所有接口
/// </summary>
[Headers("Host: api.bilibili.com")]
public interface IApiApi
{
    #region UpInfo

    /// <summary>
    /// 获取用户空间信息
    /// </summary>
    [Headers("Referer: https://www.bilibili.com/", "Origin: https://www.bilibili.com")]
    [Get("/x/space/wbi/acc/info")]
    Task<BiliApiResponse<GetSpaceInfoResponse>> GetSpaceInfo(
        [Query] GetSpaceInfoDto request,
        [Header("Cookie")] string ck
    );

    #endregion

    #region 每日任务

    /// <summary>
    /// 获取每日任务的完成情况
    /// </summary>
    [Headers(
        "Referer: https://account.bilibili.com/account/home",
        "Origin: https://account.bilibili.com"
    )]
    [Get("/x/member/web/exp/reward")]
    Task<BiliApiResponse<DailyTaskInfo>> GetDailyTaskRewardInfoAsync([Header("Cookie")] string ck);

    /// <summary>
    /// 获取通过投币已获取的经验值
    /// </summary>
    [Headers("Referer: https://www.bilibili.com/", "Origin: https://www.bilibili.com")]
    [Get("/x/web-interface/coin/today/exp")]
    Task<BiliApiResponse<int>> GetDonateCoinExpAsync([Header("Cookie")] string ck);

    /// <summary>
    /// 获取VIP特权
    /// </summary>
    [Post("/x/vip/privilege/receive?type={type}&csrf={csrf}")]
    Task<BiliApiResponse> ReceiveVipPrivilegeAsync(
        int type,
        string csrf,
        [Header("Cookie")] string ck
    );

    #endregion

    #region 关注

    /// <summary>
    /// 获取关注列表
    /// </summary>
    [Headers("Referer: https://space.bilibili.com/")]
    [Get("/x/relation/followings")]
    Task<BiliApiResponse<GetFollowingsResponse>> GetFollowings(
        [Query] GetFollowingsRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 获取特别关注列表
    /// </summary>
    [Headers("Cache-Control: no-cache", "Pragma: no-cache", "Referer: https://space.bilibili.com/")]
    [Get("/x/relation/tag")]
    Task<BiliApiResponse<List<UpInfoDto>>> GetFollowingsByTag(
        [Query] GetSpecialFollowingsRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 获取关注分组
    /// </summary>
    [Headers(
        "Sec-Fetch-Mode: no-cors",
        "Sec-Fetch-Dest: script",
        "Referer: https://space.bilibili.com/"
    )]
    [Get("/x/relation/tags?jsonp=jsonp")]
    Task<BiliApiResponse<List<TagDto>>> GetTags(
        [Header("Cookie")] string ck,
        [Header("Referer")] string referer = RelationApiConstant.GetTagsReferer
    );

    /// <summary>
    /// 添加关注分组（tag）
    /// </summary>
    [Headers("Origin: https://space.bilibili.com", "Referer: https://space.bilibili.com/")]
    [Post("/x/relation/tag/create?cross_domain=true")]
    Task<BiliApiResponse<CreateTagResponse>> CreateTag(
        [Body(BodySerializationMethod.UrlEncoded)] CreateTagRequest request,
        [Header("Cookie")] string ck,
        [Header("Referer")] string referer = RelationApiConstant.GetTagsReferer
    );

    /// <summary>
    /// 批量拷贝关注up到某指定分组
    /// </summary>
    [Headers("Origin: https://space.bilibili.com", "Referer: https://space.bilibili.com/")]
    [Post("/x/relation/tags/copyUsers")]
    Task<BiliApiResponse> CopyUpsToGroup(
        [Body(BodySerializationMethod.UrlEncoded)] CopyUserToGroupRequest request,
        [Header("Cookie")] string ck,
        [Header("Referer")] string referer = RelationApiConstant.CopyReferer
    );

    /// <summary>
    /// 修改关系
    /// </summary>
    [Headers("Origin: https://space.bilibili.com", "Referer: https://space.bilibili.com/")]
    [Post("/x/relation/modify")]
    Task<BiliApiResponse> ModifyRelation(
        [Body(BodySerializationMethod.UrlEncoded)] ModifyRelationRequest request,
        [Header("Cookie")] string ck,
        [Header("Referer")] string referer = RelationApiConstant.ModifyReferer
    );

    #endregion

    #region 充电

    /// <summary>
    /// 充电
    /// </summary>
    [Post(
        "/x/ugcpay/trade/elec/pay/quick?elec_num={elec_num}&up_mid={up_mid}&otype=up&oid={oid}&csrf={csrf}"
    )]
    [Obsolete]
    Task<BiliApiResponse<ChargeResponse>> Charge(
        int elec_num,
        string up_mid,
        string oid,
        string csrf,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 充电V2
    /// </summary>
    [Headers(
        "Content-Type: application/x-www-form-urlencoded",
        "Referer: https://www.bilibili.com/",
        "Origin: https://www.bilibili.com"
    )]
    [Post("/x/ugcpay/web/v2/trade/elec/pay/quick")]
    Task<BiliApiResponse<ChargeV2Response>> ChargeV2Async(
        [Body(BodySerializationMethod.UrlEncoded)] ChargeRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 充电后留言
    /// </summary>
    [Headers(
        "Content-Type: application/x-www-form-urlencoded",
        "Referer: https://www.bilibili.com/",
        "Origin: https://www.bilibili.com"
    )]
    [Post("/x/ugcpay/trade/elec/message")]
    Task<BiliApiResponse<ChargeResponse>> ChargeCommentAsync(
        [Body(BodySerializationMethod.UrlEncoded)] ChargeCommentRequest request,
        [Header("Cookie")] string ck
    );

    #endregion

    #region 视频

    /// <summary>
    /// 分享视频
    /// </summary>
    /// <remarks>ck中必须要有buvid3，否则几率性-403</remarks>
    [Headers("Origin: https://www.bilibili.com")]
    [Post("/x/web-interface/share/add")]
    Task<BiliApiResponse> ShareVideo(
        [Body(BodySerializationMethod.UrlEncoded)] ShareVideoRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 上传视频观看进度
    /// 每15秒上报一次
    /// </summary>
    [Headers(
        "Content-Type: application/x-www-form-urlencoded; charset=UTF-8",
        "Referer: https://www.bilibili.com/",
        "Origin: https://www.bilibili.com"
    )]
    [Post("/x/click-interface/web/heartbeat?aid={aid}&played_time={playedTime}")]
    Task<BiliApiResponse> UploadVideoHeartbeat(
        long aid,
        int playedTime,
        [Body(BodySerializationMethod.UrlEncoded)] UploadVideoHeartbeatRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 为视频投币
    /// </summary>
    [Headers("Content-Type: application/x-www-form-urlencoded", "Origin: https://www.bilibili.com")]
    [Post("/x/web-interface/coin/add")]
    Task<BiliApiResponse> AddCoinForVideo(
        [Body(BodySerializationMethod.UrlEncoded)] AddCoinRequest request,
        [Header("Cookie")] string ck,
        [Header("referer")]
            string refer =
            "https://www.bilibili.com/video/BV123456/?spm_id_from=333.1007.tianma.1-1-1.click&vd_source=80c1601a7003934e7a90709c18dfcffd"
    );

    /// <summary>
    /// 获取当前用户对视频的投币信息
    /// </summary>
    [Headers("Referer: https://www.bilibili.com/")]
    [Get("/x/web-interface/archive/coins")]
    Task<BiliApiResponse<DonatedCoinsForVideo>> GetDonatedCoinsForVideo(
        [Query] GetAlreadyDonatedCoinsRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 搜索指定Up的视频列表
    /// </summary>
    [Headers("Referer: https://www.bilibili.com/", "Origin: https://space.bilibili.com")]
    [Get("/x/space/wbi/arc/search")]
    Task<BiliApiResponse<SearchUpVideosResponse>> SearchVideosByUpId(
        [Query] SearchVideosByUpIdDto request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 通过ssid获取番剧的具体信息
    /// </summary>
    [Get("/pgc/view/web/season?season_id={ssid}")]
    Task<GetBangumiBySsidResponse> GetBangumiBySsid(long ssid, [Header("Cookie")] string ck);

    /// <summary>
    /// 获取视频详情（不需要传递Cookie）
    /// </summary>
    [Get("/x/web-interface/view?aid={aid}")]
    Task<BiliApiResponse<VideoDetail>> GetVideoDetail(string aid);

    /// <summary>
    /// 获取排行榜
    /// </summary>
    [Headers("Referer: https://www.bilibili.com/", "Origin: https://www.bilibili.com", "dnt: 1")]
    [Get("/x/web-interface/ranking/v2?rid=0&type=all")]
    Task<BiliApiResponse<Ranking>> GetRegionRankingVideosV2();

    #endregion

    #region 专栏

    [Headers("Referer: https://www.bilibili.com/", "Origin: https://space.bilibili.com")]
    [Get("/x/space/wbi/article")]
    Task<BiliApiResponse<SearchUpArticlesResponse>> SearchUpArticlesByUpIdAsync(
        [Query] SearchArticlesByUpIdDto request
    );

    /// <summary>
    /// 获取专栏详情
    /// </summary>
    [Get("/x/article/viewinfo?id={cvid}")]
    Task<BiliApiResponse<SearchArticleInfoResponse>> SearchArticleInfoAsync(long cvid);

    /// <summary>
    /// 为专栏文章投币
    /// </summary>
    [Headers("Content-Type: application/x-www-form-urlencoded", "Origin: https://www.bilibili.com")]
    [Post("/x/web-interface/coin/add")]
    Task<BiliApiResponse> AddCoinForArticleAsync(
        [Body(BodySerializationMethod.UrlEncoded)] AddCoinForArticleRequest request,
        [Header("Cookie")] string ck,
        [Header("referer")]
            string refer =
            "https://www.bilibili.com/read/cv5806746/?from=search&spm_id_from=333.337.0.0"
    );

    /// <summary>
    /// 为专栏文章点赞
    /// </summary>
    [Headers(
        "Content-Type: application/x-www-form-urlencoded",
        "Referer: https://www.bilibili.com/read/cv{cvid}/?from=search&spm_id_from=333.337.0.0",
        "Origin: https://www.bilibili.com"
    )]
    [Post("/x/article/like?id={cvid}&type=1&csrf={csrf}")]
    Task<BiliApiResponse> LikeAsync(long cvid, string csrf, [Header("Cookie")] string ck);

    #endregion

    #region 大会员积分

    /// <summary>
    /// 获取签到信息
    /// </summary>
    [Headers("Referer: https://big.bilibili.com/mobile/bigPoint/task")]
    [Get("/x/vip/vip_center/sign_in/three_days_sign")]
    Task<BiliApiResponse<ThreeDaySignResponse>> GetThreeDaySignAsync(
        [Query] ThreeDaySignRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 获取任务列表
    /// </summary>
    /// <remarks>里面的登录信息是错误的，阿B特色</remarks>
    [Headers("Referer: https://big.bilibili.com/mobile/bigPoint/task")]
    [Get("/x/vip_point/task/combine")]
    Task<BiliApiResponse<VipBigPointCombine>> GetCombineAsync(
        [Query] GetCombineRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 签到任务（旧版）
    /// </summary>
    [Obsolete("Using Sign2Async instead.")]
    [Headers("Referer: https://big.bilibili.com/mobile/bigPoint/task")]
    [Post("/pgc/activity/score/task/sign")]
    Task<BiliApiResponse> VipBigPointSignAsync(
        [Body(BodySerializationMethod.UrlEncoded)] SignRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 签到任务（新版）
    /// </summary>
    [Headers("Referer: https://big.bilibili.com/mobile/index")]
    [Post("/pgc/activity/score/task/sign2")]
    Task<BiliApiResponse<Sign2Response>> Sign2Async(
        [Query] Sign2RequestPath requestPath,
        [Body] Sign2Request request,
        [Header("Cookie")] string ck
    );

    [Obsolete]
    [Headers("Referer: https://big.bilibili.com/mobile/bigPoint/task")]
    [Post("/pgc/activity/score/task/receive")]
    Task<BiliApiResponse> VipBigPointReceive(
        [Body] ReceiveOrCompleteTaskRequest request,
        [Header("Cookie")] string ck
    );

    [Headers("Referer: https://big.bilibili.com/mobile/bigPoint/task")]
    [Post("/pgc/activity/score/task/receive/v2")]
    Task<BiliApiResponse> VipBigPointReceiveV2(
        [Body(BodySerializationMethod.UrlEncoded)] ReceiveOrCompleteTaskRequest request,
        [Header("Cookie")] string ck
    );

    [Headers("Referer: https://big.bilibili.com/mobile/bigPoint/task")]
    [Post("/pgc/activity/score/task/complete")]
    Task<BiliApiResponse> VipBigPointCompleteAsync(
        [Body] ReceiveOrCompleteTaskRequest request,
        [Header("Cookie")] string ck
    );

    [Headers("Referer: https://big.bilibili.com/mobile/bigPoint/task")]
    [Post("/pgc/activity/score/task/complete/v2")]
    Task<BiliApiResponse> VipBigPointCompleteV2(
        [Body(BodySerializationMethod.UrlEncoded)] ReceiveOrCompleteTaskRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 完成浏览页面任务
    /// </summary>
    [Headers("Referer: https://big.bilibili.com/mobile/bigPoint/task")]
    [Post("/pgc/activity/deliver/task/complete")]
    Task<BiliApiResponse> VipBigPointViewComplete(
        [Body(BodySerializationMethod.UrlEncoded)] ViewRequest request,
        [Header("Cookie")] string ck
    );

    [Headers("Referer: https://big.bilibili.com/mobile/bigPoint/task")]
    [Get("/x/vip/privilege/my")]
    Task<BiliApiResponse<VouchersInfoResponse>> GetVouchersInfoAsync([Header("Cookie")] string ck);

    /// <summary>
    /// 兑换大会员经验
    /// </summary>
    [Headers("Referer: https://big.bilibili.com/mobile/bigPoint/task")]
    [Post("/x/vip/experience/add")]
    Task<BiliApiResponse> ObtainVipExperienceAsync(
        [Body(BodySerializationMethod.UrlEncoded)] VipExperienceRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 开始观看剧集任务
    /// NOTE: HTTP verb unknown — stub method without Refit verb attribute
    /// </summary>
    Task<BiliApiResponse<StartOgvWatchResponse>> StartOgvWatchAsync(
        StartOgvWatchRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 完成观看剧集任务
    /// NOTE: HTTP verb unknown — stub method without Refit verb attribute
    /// </summary>
    Task<BiliApiResponse> CompleteOgvWatchAsync(
        CompleteOgvWatchRequest request,
        [Header("Cookie")] string ck
    );

    #endregion
}
