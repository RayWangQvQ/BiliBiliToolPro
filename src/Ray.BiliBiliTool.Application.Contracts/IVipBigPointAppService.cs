using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Application.Contracts
{
    /// <summary>
    /// 每日自动任务
    /// </summary>
    [Description("VipBigPoint")]

    public interface IVipBigPointAppService : IAppService
    {
        Task VipExpress();
    }
}
