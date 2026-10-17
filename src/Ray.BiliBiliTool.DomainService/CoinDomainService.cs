using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService;

/// <summary>
/// 硬币
/// </summary>
public class CoinDomainService(IAccountApi accountApi, IApiApi apiApi) : ICoinDomainService
{
    /// <summary>
    /// 获取账户硬币余额
    /// </summary>
    /// <returns></returns>
    public async Task<decimal> GetCoinBalance(BiliCookie ck)
    {
        var response = await accountApi.GetCoinBalanceAsync(ck.ToString());
        if (response is null || response.Code != 0 || response.Data is null)
            throw new InvalidOperationException($"获取硬币余额失败：{response?.Message}");
        return response.Data.Money ?? 0;
    }

    /// <summary>
    /// 获取今日已投币数
    /// </summary>
    /// <returns></returns>
    public async Task<int> GetDonatedCoins(BiliCookie ck)
    {
        return (await GetDonateCoinExp(ck)) / 10;
    }

    #region private
    /// <summary>
    /// 获取今日通过投币已获取的经验值
    /// </summary>
    /// <returns></returns>
    private async Task<int> GetDonateCoinExp(BiliCookie ck)
    {
        var response = await apiApi.GetDonateCoinExpAsync(ck.ToString());
        if (response is null || response.Code != 0)
            throw new InvalidOperationException($"获取投币经验失败：{response?.Message}");
        return response.Data;
    }
    #endregion
}
