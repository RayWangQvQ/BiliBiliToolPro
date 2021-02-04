using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    public interface IAccountApi : IBiliBiliApi
    {
        /// <summary>
        /// 获取硬币余额
        /// </summary>
        /// <returns></returns>
        [Get("/site/getCoin")]
        Task<BiliApiResponse<CoinBalance>> GetCoinBalance();
    }
}
