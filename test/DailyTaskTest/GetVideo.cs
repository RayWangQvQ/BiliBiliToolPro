using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace DailyTaskTest
{
    public class GetVideo
    {
        public GetVideo()
        {
            //Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });
        }

        [Fact]
        public void GetRanking()
        {
            Program.CreateHost(new string[] { });

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var dailyTaskService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();

                var re = dailyTaskService.GetRandomVideoOfRanking();

                Assert.NotNull(re);
            }
        }

        [Fact]
        public void GetVideoOfUp()
        {
            Program.CreateHost(new string[] { });

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var dailyTaskService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();

                var re = dailyTaskService.GetRandomVideoOfUp(220893216, 10);

                Assert.NotNull(re);
            }
        }

        [Fact]
        public void GetVideoInfo()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IVideoWithoutCookieApi>();

                //var re = service.GetVideoDetail("246364184").Result;//×ÔÖÆ
                var re = service.GetVideoDetail("373987080").Result;//×ªÔØ
            }

            Assert.True(true);
        }
    }
}
