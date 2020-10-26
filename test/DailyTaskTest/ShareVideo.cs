using DailyTaskTest.Share;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Xunit;

namespace ShareVideoTest
{
    public class ShareVideo
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = Program.ServiceProviderRoot.CreateScope())
            {
                var dailyTaskService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();

                string aid = dailyTaskService.GetRandomVideo();
                dailyTaskService.ShareVideo(aid);

                Assert.True(true);
            }
        }
    }
}
