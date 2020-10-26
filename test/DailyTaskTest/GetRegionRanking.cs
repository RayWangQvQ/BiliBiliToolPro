using DailyTaskTest.Share;
using Ray.BiliBiliTool.Console;
using Xunit;

namespace GetRegionRankingTest
{
    public class GetRegionRanking
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            var dailyTask = DailyTaskBuilder.Build();
            var re = dailyTask.GetRandomVideo();

            Assert.NotNull(re);
        }
    }
}
