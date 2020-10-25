using System;
using BiliBiliTool;
using BiliBiliTool.Task;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GetRegionRankingTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            DailyTask dailyTask = Program.ServiceProviderRoot.GetRequiredService<DailyTask>();
            var re = dailyTask.GetRandomVideo();

            Assert.NotNull(re);
        }
    }
}
