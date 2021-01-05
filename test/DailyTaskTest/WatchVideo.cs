using System;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace WatchVideoTest
{
    public class WatchVideo
    {
        public WatchVideo()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.PreWorks(new string[] { });
        }

        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var domainService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();
                var account = scope.ServiceProvider.GetRequiredService<IAccountDomainService>();

                //var dailyTaskStatus = account.GetDailyTaskStatus();
                //var aid = domainService.GetRandomVideoOfRegion().Item1;
                domainService.WatchVideo("843414547");

                Assert.True(true);
            }
        }
    }
}
