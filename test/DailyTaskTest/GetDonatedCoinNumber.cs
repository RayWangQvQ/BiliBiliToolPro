using System;
using DailyTaskTest.Share;
using Xunit;

namespace DailyTaskTest
{
    public class GetDonatedCoinNumber
    {
        [Fact]
        public void Test1()
        {
            var dailyTaskAppService = DailyTaskBuilder.Build();

            int number = dailyTaskAppService.GetDonateCoins();

            Assert.True(number >= 0);
        }
    }
}
