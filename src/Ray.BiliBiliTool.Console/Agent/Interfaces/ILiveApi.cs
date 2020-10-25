using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BiliBiliTool.Agent;
using Refit;

namespace Ray.BiliBiliTool.Console.Agent.Interfaces
{
    public interface ILiveApi : IBiliBiliApi
    {
        [Get("/pay/v1/Exchange/silver2coin")]
        Task<BiliApiResponse> ExchangeSilver2Coin();

        [Get("/pay/v1/Exchange/getStatus")]
        Task<BiliApiResponse<ExchangeSilverStatusResponse>> GetExchangeSilverStatus();
    }
}
