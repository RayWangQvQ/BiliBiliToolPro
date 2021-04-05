using System;
using System.Collections.Generic;
using System.Text;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// 直播中心
    /// </summary>
    public interface ILiveDomainService : IDomainService
    {
        /// <summary>
        /// 签到
        /// </summary>
        void LiveSign();

        /// <summary>
        /// 银瓜子兑换硬币
        /// </summary>
        /// <returns></returns>
        bool ExchangeSilver2Coin();

        /// <summary>
        /// 天选抽奖
        /// </summary>
        void TianXuan();

        void TryJoinTianXuan(ListItemDto target);

        void GroupFollowing();
    }
}
