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
            Program.PreWorks(new BiliBiliTool.Login.Verify("220893216",
                "41bd553c%2C1612573459%2Ca8673*81",
                "b85b804da65ef6454a153e7606d8d967"));

            return Program.ServiceProviderRoot.GetRequiredService<DailyTask>();
        }
    }
}
