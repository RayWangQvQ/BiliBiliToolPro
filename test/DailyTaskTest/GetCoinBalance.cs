using System;
using DailyTaskTest.Share;
using Xunit;

namespace DailyTaskTest
{
    public class GetCoinBalance
    {
        [Fact]
        public void Test1()
        {
            var dailyTaskAppService = DailyTaskBuilder.Build();

            int number = dailyTaskAppService.GetCoinBalance();

            Assert.True(number >= 0);
        }
    }
}
