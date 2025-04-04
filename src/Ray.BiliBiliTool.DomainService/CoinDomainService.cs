using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService;

/// <summary>
/// 硬币
/// </summary>
public class CoinDomainService(IAccountApi accountApi, IDailyTaskApi dailyTaskApi)
    : ICoinDomainService
{
    /// <summary>
    /// 获取账户硬币余额
    /// </summary>
    /// <returns></returns>
    public async Task<decimal> GetCoinBalance()
    {
        var response = await accountApi.GetCoinBalanceAsync();
        return response.Data.Money ?? 0;
    }

    /// <summary>
    /// 获取今日已投币数
    /// </summary>
    /// <returns></returns>
    public async Task<int> GetDonatedCoins()
    {
        return (await GetDonateCoinExp()) / 10;
    }

    #region private
    /// <summary>
    /// 获取今日通过投币已获取的经验值
    /// </summary>
    /// <returns></returns>
    private async Task<int> GetDonateCoinExp()
    {
        return (await dailyTaskApi.GetDonateCoinExpAsync()).Data;
    }
    #endregion
}
