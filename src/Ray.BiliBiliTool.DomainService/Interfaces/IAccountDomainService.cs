using System;
using System.Collections.Generic;
using System.Text;
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
        UserInfo LoginByCookie();

        /// <summary>
        /// 获取每日任务完成情况
        /// </summary>
        /// <returns></returns>
        DailyTaskInfo GetDailyTaskStatus();

        /// <summary>
        /// 批量取关
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="count"></param>
        void UnfollowBatched(string groupName, int count);
    }
}
