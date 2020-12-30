using System;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace DailyTaskTest
{
    public class ReceiveVipPrivilege
    {
        public ReceiveVipPrivilege()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.PreWorks(null);
        }

        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var dailyTask = scope.ServiceProvider.GetRequiredService<IVipPrivilegeDomainService>();
                var account = scope.ServiceProvider.GetRequiredService<IAccountDomainService>();

                var userInfo = account.LoginByCookie();
                dailyTask.ReceiveVipPrivilege(userInfo);

                Assert.True(true);
            }
        }
    }
}
