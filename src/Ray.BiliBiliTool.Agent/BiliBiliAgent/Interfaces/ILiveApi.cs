using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 直播相关接口
    /// </summary>
    public interface ILiveApi : IBiliBiliApi
    {
        /// <summary>
        /// 直播签到
        /// </summary>
        /// <returns></returns>
        [Get("/xlive/web-ucenter/v1/sign/DoSign")]
        Task<BiliApiResponse<LiveSignResponse>> Sign();

        /// <summary>
        /// 银瓜子兑换硬币
        /// </summary>
        /// <returns></returns>
        [Get("/pay/v1/Exchange/silver2coin")]
        Task<BiliApiResponse> ExchangeSilver2Coin();

        /// <summary>
        /// 获取银瓜子余额
        /// </summary>
        /// <returns></returns>
        [Get("/pay/v1/Exchange/getStatus")]
        Task<BiliApiResponse<ExchangeSilverStatusResponse>> GetExchangeSilverStatus();
    }
}
