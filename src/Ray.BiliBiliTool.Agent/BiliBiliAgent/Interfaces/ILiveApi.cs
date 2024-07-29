using System;
using System.Net.Http;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

/// <summary>
/// 直播相关接口
/// </summary>
[Header("Host", "api.live.bilibili.com")]

public interface ILiveApi : IBiliBiliApi
{
    /// <summary>
    /// 直播签到
    /// </summary>
    /// <returns></returns>
    [Header("Referer", "https://link.bilibili.com/")]
    [Header("Origin", "https://link.bilibili.com")]
    [HttpGet("/xlive/web-ucenter/v1/sign/DoSign")]
    Task<BiliApiResponse<LiveSignResponse>> Sign();

    /// <summary>
    /// 银瓜子兑换硬币
    /// </summary>
    /// <returns></returns>
    [Header("Referer", "https://link.bilibili.com/")]
    [Header("Origin", "https://link.bilibili.com")]
    [Header("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8")]
    [HttpGet("/pay/v1/Exchange/silver2coin")]
    [Obsolete]
    Task<BiliApiResponse> ExchangeSilver2Coin();

    /// <summary>
    /// 获取银瓜子余额
    /// </summary>
    /// <returns></returns>
    [Header("Referer", "https://link.bilibili.com/")]
    [Header("Origin", "https://link.bilibili.com")]
    [HttpGet("/pay/v1/Exchange/getStatus")]
    [Obsolete]
    Task<BiliApiResponse<ExchangeSilverStatusResponse>> GetExchangeSilverStatus();

    /// <summary>
    /// 银瓜子兑换硬币
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    //[Header("Referer", "https://link.bilibili.com/p/center/index?visit_id=1ddo4yl01q00")]
    [Header("Content-Type", "application/x-www-form-urlencoded")]
    [Header("Origin", "https://link.bilibili.com")]
    [HttpPost("/xlive/revenue/v1/wallet/silver2coin")]
    Task<BiliApiResponse<Silver2CoinResponse>> Silver2Coin([FormContent] Silver2CoinRequest request);

    /// <summary>
    /// 获取直播中心钱包状态
    /// </summary>
    /// <returns></returns>
    //[Header("Referer", "https://link.bilibili.com/p/center/index?visit_id=1ddo4yl01q00")]
    [Header("Origin", "https://link.bilibili.com")]
    [HttpGet("/xlive/revenue/v1/wallet/getStatus")]
    Task<BiliApiResponse<LiveWalletStatusResponse>> GetLiveWalletStatus();

    [HttpGet("/xlive/web-interface/v1/index/getWebAreaList?source_id=2")]
    Task<BiliApiResponse<GetArteaListResponse>> GetAreaList();

    /// <summary>
    /// 获取直播列表
    /// </summary>
    /// <param name="parentAreaId"></param>
    /// <param name="page"></param>
    /// <param name="areaId"></param>
    /// <param name="sortType">sort_type_124</param>
    /// <returns></returns>
    [Header("Referer", "https://live.bilibili.com/")]
    [Header("Origin", "https://live.bilibili.com")]
    [HttpGet("/xlive/web-interface/v1/second/getList?platform=web&parent_area_id={parentAreaId}&area_id={areaId}&sort_type={sortType}&page={page}")]
    Task<BiliApiResponse<GetListResponse>> GetList(long parentAreaId, int page, int areaId = 0, string sortType = "");
    //todo:Cookie比nav接口多了两项：Hm_lvt_8a6e55dbd2870f0f5bc9194cddf32a02、Hm_lvt_9e2a88dc69e0e55c353597501d2a4bbc

    /// <summary>
    /// 检查天选时刻抽奖
    /// </summary>
    /// <param name="roomId"></param>
    /// <returns></returns>
    [Header("Referer", "https://live.bilibili.com/")]
    [Header("Origin", "https://live.bilibili.com")]
    [HttpGet("/xlive/lottery-interface/v1/Anchor/Check?roomid={roomId}")]
    Task<BiliApiResponse<CheckTianXuanDto>> CheckTianXuan(long roomId);

    /// <summary>
    /// 参加天选时刻抽奖
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("/xlive/lottery-interface/v1/Anchor/Join")]
    Task<BiliApiResponse<JoinTianXuanResponse>> Join([FormContent] JoinTianXuanRequest request);

    /// <summary>
    /// 获取用户的粉丝勋章
    /// </summary>
    /// <param name="userId">uid</param>
    /// <returns></returns>
    [Header("Referer", "https://live.bilibili.com/")]
    [Header("Origin", "https://live.bilibili.com")]
    [HttpGet("/xlive/web-ucenter/user/MedalWall?target_id={userId}")]
    Task<BiliApiResponse<MedalWallResponse>> GetMedalWall(string userId);

    /// <summary>
    /// 佩戴粉丝勋章
    /// </summary>
    /// <param name="userId">uid</param>
    /// <returns></returns>
    [Header("Referer", "https://live.bilibili.com/")]
    [Header("Origin", "https://live.bilibili.com")]
    [HttpPost("/xlive/app-ucenter/v1/fansMedal/wear")]
    Task<BiliApiResponse> WearMedalWall([FormContent] WearMedalWallRequest request);

    /// <summary>
    /// 发送弹幕
    /// </summary>
    /// <param name="request">request</param>
    /// <returns></returns>
    [HttpPost("/msg/send")]
    Task<BiliApiResponse> SendLiveDanmuku([FormContent] SendLiveDanmukuRequest request);

    /// <summary>
    /// 获取直播间信息
    /// </summary>
    /// <param name="roomId">roomId</param>
    /// <returns></returns>
    [HttpGet("/room/v1/Room/get_info?room_id={roomId}&from=room")]
    Task<BiliApiResponse<GetLiveRoomInfoResponse>> GetLiveRoomInfo(long roomId);

    /// <summary>
    /// 请求直播主页用于配置直播相关 Cookie
    /// </summary>
    [HttpGet("/news/v1/notice/recom?product=live")]
    Task<HttpResponseMessage> GetLiveHome();

    /// <summary>
    /// 点赞直播间
    /// </summary>
    [HttpPost("/xlive/app-ucenter/v1/like_info_v3/like/likeReportV3")]
    [Header("Referer", "https://live.bilibili.com/")]
    [Header("Origin", "https://live.bilibili.com")]
    Task<BiliApiResponse> LikeLiveRoom([RawFormContent] string request);
}