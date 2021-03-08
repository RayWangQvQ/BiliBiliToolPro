using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
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
        Task<BiliApiResponse> ExchangeSilver2Coin();

        /// <summary>
        /// 获取银瓜子余额
        /// </summary>
        /// <returns></returns>
        [Header("Referer", "https://link.bilibili.com/")]
        [Header("Origin", "https://link.bilibili.com")]
        [HttpGet("/pay/v1/Exchange/getStatus")]
        Task<BiliApiResponse<ExchangeSilverStatusResponse>> GetExchangeSilverStatus();

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
        Task<BiliApiResponse<GetListResponse>> GetList(int parentAreaId, int page, int areaId = 0, string sortType = "");
        //todo:Cookie比nav接口多了两项：Hm_lvt_8a6e55dbd2870f0f5bc9194cddf32a02、Hm_lvt_9e2a88dc69e0e55c353597501d2a4bbc

        /// <summary>
        /// 检查天选时刻抽奖
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [Header("Referer", "https://live.bilibili.com/")]
        [Header("Origin", "https://live.bilibili.com")]
        [HttpGet("/xlive/lottery-interface/v1/Anchor/Check?roomid={roomId}")]
        Task<BiliApiResponse<CheckTianXuanDto>> CheckTianXuan(int roomId);

        /// <summary>
        /// 参加天选时刻抽奖
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("/xlive/lottery-interface/v1/Anchor/Join")]
        Task<BiliApiResponse<JoinTianXuanResponse>> Join([FormContent] JoinTianXuanRequest request);
    }
}
