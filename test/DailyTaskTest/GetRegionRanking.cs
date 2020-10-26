using DailyTaskTest.Share;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Xunit;

namespace GetRegionRankingTest
{
    public class GetRegionRanking
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = Program.ServiceProviderRoot.CreateScope())
            {
                var dailyTaskService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();

                var re = dailyTaskService.GetRandomVideo();

                Assert.NotNull(re);
            }
        }
    }
}
