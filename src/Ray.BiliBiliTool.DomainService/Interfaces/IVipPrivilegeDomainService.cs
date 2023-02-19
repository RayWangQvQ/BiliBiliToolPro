using System.Threading.Tasks;
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
        Task<bool> ReceiveVipPrivilege(UserInfo userInfo);
    }
}
