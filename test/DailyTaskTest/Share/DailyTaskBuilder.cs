using System;
using System.Collections.Generic;
using System.Text;
using BiliBiliTool;
using BiliBiliTool.Task;
using Microsoft.Extensions.DependencyInjection;

namespace DailyTaskTest.Share
{
    public class DailyTaskBuilder
    {
        public static DailyTask Build()
        {
            Program.PreWorks(new string[] { });

            return Program.ServiceProviderRoot.GetRequiredService<DailyTask>();
        }
    }
}
