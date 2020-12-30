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
        public Charge()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.PreWorks(new string[] { });
        }

        [Fact]
        public void Test1()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
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
