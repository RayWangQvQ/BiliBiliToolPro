using System;
using System.Collections.Generic;
using System.Text;
using BiliBiliTool;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Application;
using Ray.BiliBiliTool.Console;

namespace DailyTaskTest.Share
{
    public class DailyTaskBuilder
    {
        public static DailyTaskAppService Build()
        {
            Program.PreWorks(new string[] { });

            return Program.ServiceProviderRoot.GetRequiredService<DailyTaskAppService>();
        }
    }
}
