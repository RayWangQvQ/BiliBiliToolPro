using System;
using DailyTaskTest.Share;
using Xunit;

namespace DailyTaskTest
{
    public class Charge
    {
        [Fact]
        public void Test1()
        {
            var dailyTaskAppService = DailyTaskBuilder.Build();

            dailyTaskAppService.Login();
            dailyTaskAppService.doCharge();

            Assert.True(true);
        }
    }
}
