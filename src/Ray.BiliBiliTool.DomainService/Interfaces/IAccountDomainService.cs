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

        /// <summary>
        /// 计算升级时间
        /// </summary>
        /// <param name="useInfo"></param>
        /// <returns>升级时间</returns>
        int CalculateUpgradeTime(UserInfo useInfo);
    }
}
