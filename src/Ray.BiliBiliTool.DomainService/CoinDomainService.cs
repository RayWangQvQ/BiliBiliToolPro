using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService
{
    /// <summary>
    /// 硬币
    /// </summary>
    public class CoinDomainService : ICoinDomainService
    {
        private readonly IAccountApi _accountApi;
        private readonly IDailyTaskApi _dailyTaskApi;

        public CoinDomainService(
            IAccountApi accountApi,
            IDailyTaskApi dailyTaskApi
            )
        {
            _accountApi = accountApi;
            _dailyTaskApi = dailyTaskApi;
        }

        /// <summary>
        /// 获取账户硬币余额
        /// </summary>
        /// <returns></returns>
        public decimal GetCoinBalance()
        {
            var response = _accountApi.GetCoinBalance().GetAwaiter().GetResult();
            return response.Data.Money ?? 0;
        }

        /// <summary>
        /// 获取今日已投币数
        /// </summary>
        /// <returns></returns>
        public int GetDonatedCoins()
        {
            return GetDonateCoinExp() / 10;
        }

        #region private
        /// <summary>
        /// 获取今日通过投币已获取的经验值
        /// </summary>
        /// <returns></returns>
        private int GetDonateCoinExp()
        {
            return _dailyTaskApi.GetDonateCoinExp().GetAwaiter().GetResult().Data;
        }
        #endregion
    }
}
