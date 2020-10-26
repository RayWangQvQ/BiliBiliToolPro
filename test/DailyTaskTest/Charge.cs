using System;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace DailyTaskTest
{
    public class Charge
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var dailyTask = scope.ServiceProvider.GetRequiredService<IChargeDomainService>();
                var accountService = scope.ServiceProvider.GetRequiredService<IAccountDomainService>();

                var userInfo = accountService.LoginByCookie();
                dailyTask.Charge(userInfo);

                Assert.True(true);
            }
        }
    }
}
