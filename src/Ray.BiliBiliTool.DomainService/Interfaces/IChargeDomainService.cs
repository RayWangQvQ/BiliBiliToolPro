using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// 充电
    /// </summary>
    public interface IChargeDomainService : IDomainService
    {
        /// <summary>
        /// 充电
        /// </summary>
        /// <param name="userInfo"></param>
        Task Charge(UserInfo userInfo);

        /// <summary>
        /// 充电后留言
        /// </summary>
        /// <param name="token"></param>
        Task ChargeComments(string token);
    }
}
