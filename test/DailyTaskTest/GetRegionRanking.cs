using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace GetRegionRankingTest
{
    public class GetRegionRanking
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var dailyTaskService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();

                var re = dailyTaskService.GetRandomVideoOfRegion();

                Assert.NotNull(re);
            }
        }
    }
}
