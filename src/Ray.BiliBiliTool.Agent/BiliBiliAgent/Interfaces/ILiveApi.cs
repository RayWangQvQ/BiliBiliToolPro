using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 直播相关接口
    /// </summary>
    [Header("Host", "api.live.bilibili.com")]
    [Header("Referer", "https://link.bilibili.com/")]
    [Header("Origin", "https://link.bilibili.com")]
    public interface ILiveApi : IBiliBiliApi
    {
        /// <summary>
        /// 直播签到
        /// </summary>
        /// <returns></returns>
        [HttpGet("/xlive/web-ucenter/v1/sign/DoSign")]
        Task<BiliApiResponse<LiveSignResponse>> Sign();

        /// <summary>
        /// 银瓜子兑换硬币
        /// </summary>
        /// <returns></returns>
        [Header("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8")]
        [HttpGet("/pay/v1/Exchange/silver2coin")]
        Task<BiliApiResponse> ExchangeSilver2Coin();

        /// <summary>
        /// 获取银瓜子余额
        /// </summary>
        /// <returns></returns>
        [HttpGet("/pay/v1/Exchange/getStatus")]
        Task<BiliApiResponse<ExchangeSilverStatusResponse>> GetExchangeSilverStatus();
    }
}
