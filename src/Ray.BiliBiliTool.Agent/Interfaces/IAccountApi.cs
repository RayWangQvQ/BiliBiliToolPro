using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.Interfaces
{
    public interface IAccountApi : IBiliBiliApi
    {
        [Get("/site/getCoin")]
        Task<BiliApiResponse<CoinBalance>> GetCoinBalance();
    }
}
