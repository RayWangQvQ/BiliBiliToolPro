using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace ShareVideoTest
{
    public class ShareVideo
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var dailyTaskService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();
                var account = scope.ServiceProvider.GetRequiredService<IAccountDomainService>();

                var dailyTaskStatus = account.GetDailyTaskStatus();

                var aid = dailyTaskService.GetRandomVideo();
                dailyTaskService.ShareVideo(aid);

                Assert.True(true);
            }
        }
    }
}
