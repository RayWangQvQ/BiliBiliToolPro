using System;
using System.Collections.Generic;
using System.Text;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// 漫画
    /// </summary>
    public interface IMangaDomainService : IDomainService
    {
        /// <summary>
        /// 签到
        /// </summary>
        void MangaSign();

        /// <summary>
        /// 获取大会员权益
        /// </summary>
        /// <param name="reason_id"></param>
        /// <param name="userIfo"></param>
        void ReceiveMangaVipReward(int reason_id, UserInfo userIfo);
    }
}
