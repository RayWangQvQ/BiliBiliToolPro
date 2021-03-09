using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.Application.Contracts
{
    /// <summary>
    /// 每日自动任务
    /// </summary>
    public interface ILiveTaskAppService : IAppService
    {
        /// <summary>
        /// 开始天选时刻抽奖任务
        /// </summary>
        void DoLotteryTask();
    }
}
