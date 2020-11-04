using System;
using System.Collections.Generic;
using System.Text;

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
        decimal GetCoinBalance();

        /// <summary>
        /// 获取今日已投币数量
        /// </summary>
        /// <returns></returns>
        int GetDonatedCoins();
    }
}
