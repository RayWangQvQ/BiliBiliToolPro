using System;
using DailyTaskTest.Share;
using Xunit;

namespace DailyTaskTest
{
    public class ExchangeSilver2Coin
    {
        [Fact]
        public void Test1()
        {
            var dailyTaskAppService = DailyTaskBuilder.Build();

            dailyTaskAppService.ExchangeSilver2Coin();

            Assert.True(true);
        }
    }
}
