using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    [Header("Host", "account.bilibili.com")]
    public interface IAccountApi : IBiliBiliApi
    {
        /// <summary>
        /// 获取硬币余额
        /// </summary>
        /// <returns></returns>
        [Header("Referer", "https://account.bilibili.com/account/coin")]
        [HttpGet("/site/getCoin")]
        Task<BiliApiResponse<CoinBalance>> GetCoinBalance();
    }
}
