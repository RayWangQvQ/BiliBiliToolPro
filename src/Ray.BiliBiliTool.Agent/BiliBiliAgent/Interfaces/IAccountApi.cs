using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.AccountApi;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

[Headers("Host: account.bilibili.com")]
public interface IAccountApi
{
    /// <summary>
    /// 获取硬币余额
    /// </summary>
    /// <returns></returns>
    [Headers("Referer: https://account.bilibili.com/account/coin")]
    [Get("/site/getCoin")]
    Task<BiliApiResponse<CoinBalance>> GetCoinBalanceAsync([Header("Cookie")] string ck);
}
