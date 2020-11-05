using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    public interface IAccountApi : IBiliBiliApi
    {
        [Get("/site/getCoin")]
        Task<BiliApiResponse<CoinBalance>> GetCoinBalance();
    }
}
