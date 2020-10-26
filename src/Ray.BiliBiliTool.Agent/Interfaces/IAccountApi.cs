using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BiliBiliTool.Agent;
using Refit;

namespace Ray.BiliBiliTool.Console.Agent.Interfaces
{
    public interface IAccountApi : IBiliBiliApi
    {
        [Get("/site/getCoin")]
        Task<BiliApiResponse<CoinBalance>> GetCoinBalance();
    }
}
