using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace WatchVideoTest
{
    public class WatchVideo
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var dailyTask = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();
                var account = scope.ServiceProvider.GetRequiredService<IAccountDomainService>();

                var dailyTaskStatus = account.GetDailyTaskStatus();
                var aid = dailyTask.GetRandomVideo();
                dailyTask.WatchVideo(aid);

                Assert.True(true);
            }
        }
    }
}
