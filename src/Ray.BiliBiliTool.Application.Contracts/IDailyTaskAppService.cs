using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.Application.Contracts
{
    /// <summary>
    /// 每日自动任务
    /// </summary>
    public interface IDailyTaskAppService : IAppService
    {
        /// <summary>
        /// 开始任务
        /// </summary>
        void DoDailyTask();
    }
}
