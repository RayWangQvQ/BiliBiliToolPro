using System.Threading.Tasks;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// B币
    /// </summary>
    public interface ICoinDomainService : IDomainService
    {
        /// <summary>
        /// 获取账户硬币余额
        /// </summary>
        /// <returns></returns>
        Task<decimal> GetCoinBalance();

        /// <summary>
        /// 获取今日已投币数量
        /// </summary>
        /// <returns></returns>
        Task<int> GetDonatedCoins();
    }
}
