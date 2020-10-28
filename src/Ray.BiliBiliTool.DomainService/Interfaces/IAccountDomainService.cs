using System;
using System.Collections.Generic;
using System.Text;
using Ray.BiliBiliTool.Agent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    public interface IAccountDomainService : IDomainService
    {
        UseInfo LoginByCookie();

        DailyTaskInfo GetDailyTaskStatus();
    }
}
