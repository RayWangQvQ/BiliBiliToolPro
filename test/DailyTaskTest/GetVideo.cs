using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace WatchVideoTest
{
    public class GetVideo
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var dailyTask = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();

                var re = dailyTask.TryGetCanDonatedVideo();

                Assert.True(re != null);
            }
        }
    }
}
