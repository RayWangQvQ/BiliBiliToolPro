using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    public interface IAccountApi : IBiliBiliApi
    {
        /// <summary>
        /// 获取硬币余额
        /// </summary>
        /// <returns></returns>
        [HttpGet("/site/getCoin")]
        Task<BiliApiResponse<CoinBalance>> GetCoinBalance();
    }
}
