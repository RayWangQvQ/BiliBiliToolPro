using System;
using DailyTaskTest.Share;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Xunit;

namespace DailyTaskTest
{
    public class ReceiveMangaVipReward
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = Program.ServiceProviderRoot.CreateScope())
            {
                var dailyTask = scope.ServiceProvider.GetRequiredService<IMangaDomainService>();
                var account = scope.ServiceProvider.GetRequiredService<IAccountDomainService>();

                var userInfo = account.LoginByCookie();
                dailyTask.ReceiveMangaVipReward(1, userInfo);

                Assert.True(true);
            }
        }
    }
}
