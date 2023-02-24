using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// 账户
    /// </summary>
    public interface IAccountDomainService : IDomainService
    {
        /// <summary>
        /// 使用Cookie登录
        /// </summary>
        /// <returns></returns>
        Task<UserInfo> LoginByCookie();

        /// <summary>
        /// 获取每日任务完成情况
        /// </summary>
        /// <returns></returns>
        Task<DailyTaskInfo> GetDailyTaskStatus();

        /// <summary>
        /// 批量取关
        /// </summary>
        Task UnfollowBatched();
    }
}
