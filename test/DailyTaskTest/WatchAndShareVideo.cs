using System;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Dtos;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace DailyTaskTest
{
    public class WatchAndShareVideo
    {
        public WatchAndShareVideo()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });
        }

        [Fact]
        public void Watch()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var domainService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();
                var account = scope.ServiceProvider.GetRequiredService<IAccountDomainService>();

                //var dailyTaskStatus = account.GetDailyTaskStatus();
                //var aid = domainService.GetRandomVideoOfRegion().Item1;
                domainService.WatchVideo(new VideoInfoDto { Aid = "220893216" });

                Assert.True(true);
            }
        }

        [Fact]
        public void Share()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var dailyTaskService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();
                var account = scope.ServiceProvider.GetRequiredService<IAccountDomainService>();

                var dailyTaskStatus = account.GetDailyTaskStatus();

                var aid = dailyTaskService.GetRandomVideoOfRanking().Aid.ToString();
                dailyTaskService.ShareVideo(new VideoInfoDto { Aid = aid });

                Assert.True(true);
            }
        }

        [Fact]
        public void WatchAndShare()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();

            service.WatchAndShareVideo(new DailyTaskInfo());

            Console.ReadLine();
        }
    }
}
