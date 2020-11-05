using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    public interface ILiveApi : IBiliBiliApi
    {
        [Get("/pay/v1/Exchange/silver2coin")]
        Task<BiliApiResponse> ExchangeSilver2Coin();

        [Get("/pay/v1/Exchange/getStatus")]
        Task<BiliApiResponse<ExchangeSilverStatusResponse>> GetExchangeSilverStatus();

        [Get("/xlive/web-ucenter/v1/sign/DoSign")]
        Task<BiliApiResponse<LiveSignResponse>> Sign();
    }
}
