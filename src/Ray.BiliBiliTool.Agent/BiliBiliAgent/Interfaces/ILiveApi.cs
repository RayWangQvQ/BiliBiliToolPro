using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

/// <summary>
/// 直播相关接口
/// </summary>
[Headers("Host: api.live.bilibili.com")]
public interface ILiveApi
{
    /// <summary>
    /// 直播签到
    /// </summary>
    /// <returns></returns>
    [Headers("Referer: https://link.bilibili.com/", "Origin: https://link.bilibili.com")]
    [Get("/xlive/web-ucenter/v1/sign/DoSign")]
    Task<BiliApiResponse<LiveSignResponse>> Sign([Header("Cookie")] string ck);

    /// <summary>
    /// 银瓜子兑换硬币
    /// </summary>
    /// <returns></returns>
    [Headers(
        "Referer: https://link.bilibili.com/",
        "Origin: https://link.bilibili.com",
        "Content-Type: application/x-www-form-urlencoded; charset=UTF-8"
    )]
    [Get("/pay/v1/Exchange/silver2coin")]
    [Obsolete]
    Task<BiliApiResponse> ExchangeSilver2Coin([Header("Cookie")] string ck);

    /// <summary>
    /// 获取银瓜子余额
    /// </summary>
    /// <returns></returns>
    [Headers("Referer: https://link.bilibili.com/", "Origin: https://link.bilibili.com")]
    [Get("/pay/v1/Exchange/getStatus")]
    [Obsolete]
    Task<BiliApiResponse<ExchangeSilverStatusResponse>> GetExchangeSilverStatus(
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 银瓜子兑换硬币
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    //[Headers("Referer: https://link.bilibili.com/p/center/index?visit_id=1ddo4yl01q00")]
    [Headers(
        "Content-Type: application/x-www-form-urlencoded",
        "Origin: https://link.bilibili.com"
    )]
    [Post("/xlive/revenue/v1/wallet/silver2coin")]
    Task<BiliApiResponse<Silver2CoinResponse>> Silver2Coin(
        [Body(BodySerializationMethod.UrlEncoded)] Silver2CoinRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 获取直播中心钱包状态
    /// </summary>
    /// <returns></returns>
    //[Headers("Referer: https://link.bilibili.com/p/center/index?visit_id=1ddo4yl01q00")]
    [Headers("Origin: https://link.bilibili.com")]
    [Get("/xlive/revenue/v1/wallet/getStatus")]
    Task<BiliApiResponse<LiveWalletStatusResponse>> GetLiveWalletStatus(
        [Header("Cookie")] string ck
    );

    [Get("/xlive/web-interface/v1/index/getWebAreaList?source_id=2")]
    Task<BiliApiResponse<GetArteaListResponse>> GetAreaList([Header("Cookie")] string ck);

    /// <summary>
    /// 获取直播列表
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ck"></param>
    /// <returns></returns>
    [Headers("Referer: https://live.bilibili.com/", "Origin: https://live.bilibili.com")]
    [Get("/xlive/web-interface/v1/second/getList")]
    Task<BiliApiResponse<GetListResponse>> GetList(
        [Query] GetListRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 检查天选时刻抽奖
    /// </summary>
    /// <param name="roomId"></param>
    /// <returns></returns>
    [Headers("Referer: https://live.bilibili.com/", "Origin: https://live.bilibili.com")]
    [Get("/xlive/lottery-interface/v1/Anchor/Check?roomid={roomId}")]
    Task<BiliApiResponse<CheckTianXuanDto>> CheckTianXuan(
        long roomId,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 参加天选时刻抽奖
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [Post("/xlive/lottery-interface/v1/Anchor/Join")]
    Task<BiliApiResponse<JoinTianXuanResponse>> Join(
        [Body(BodySerializationMethod.UrlEncoded)] JoinTianXuanRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 获取用户的粉丝勋章
    /// </summary>
    /// <param name="userId">uid</param>
    /// <returns></returns>
    [Headers("Referer: https://live.bilibili.com/", "Origin: https://live.bilibili.com")]
    [Get("/xlive/web-ucenter/user/MedalWall?target_id={userId}")]
    Task<BiliApiResponse<MedalWallResponse>> GetMedalWall(
        string userId,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 佩戴粉丝勋章
    /// </summary>
    /// <param name="userId">uid</param>
    /// <returns></returns>
    [Headers("Referer: https://live.bilibili.com/", "Origin: https://live.bilibili.com")]
    [Post("/xlive/app-ucenter/v1/fansMedal/wear")]
    Task<BiliApiResponse> WearMedalWall(
        [Body(BodySerializationMethod.UrlEncoded)] WearMedalWallRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 发送弹幕
    /// </summary>
    /// <param name="request">request</param>
    /// <returns></returns>
    [Post("/msg/send")]
    Task<BiliApiResponse> SendLiveDanmuku(
        [Body(BodySerializationMethod.UrlEncoded)] SendLiveDanmukuRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 获取直播间信息
    /// </summary>
    /// <param name="roomId">roomId</param>
    /// <returns></returns>
    [Get("/room/v1/Room/get_info?room_id={roomId}&from=room")]
    Task<BiliApiResponse<GetLiveRoomInfoResponse>> GetLiveRoomInfo(long roomId);

    /// <summary>
    /// 请求直播主页用于配置直播相关 Cookie
    /// </summary>
    [Get("/news/v1/notice/recom?product=live")]
    Task<HttpResponseMessage> GetLiveHome([Header("Cookie")] string ck);

    /// <summary>
    /// 点赞直播间
    /// </summary>
    [Headers(
        "Referer: https://live.bilibili.com/",
        "Origin: https://live.bilibili.com",
        "Content-Type: application/x-www-form-urlencoded"
    )]
    [Post("/xlive/app-ucenter/v1/like_info_v3/like/likeReportV3")]
    Task<BiliApiResponse> LikeLiveRoom([Body] string request, [Header("Cookie")] string ck);
}
