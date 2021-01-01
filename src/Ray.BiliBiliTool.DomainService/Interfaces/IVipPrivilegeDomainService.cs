using System;
using System.Collections.Generic;
using System.Text;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// 大会员权益
    /// </summary>
    public interface IVipPrivilegeDomainService : IDomainService
    {
        /// <summary>
        /// 获取大会员权益
        /// </summary>
        /// <param name="useInfo"></param>
        bool ReceiveVipPrivilege(UserInfo userInfo);
    }
}
