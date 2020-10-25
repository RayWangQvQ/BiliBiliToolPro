using System;
using DailyTaskTest.Share;
using Xunit;

namespace DailyTaskTest
{
    public class ReceiveMangaVipReward
    {
        [Fact]
        public void Test1()
        {
            var dailyTaskAppService = DailyTaskBuilder.Build();

            dailyTaskAppService.ReceiveMangaVipReward(1);

            Assert.True(true);
        }
    }
}
