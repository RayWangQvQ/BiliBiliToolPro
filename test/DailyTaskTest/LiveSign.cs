using System;
using DailyTaskTest.Share;
using Xunit;

namespace DailyTaskTest
{
    public class LiveSign
    {
        [Fact]
        public void Test1()
        {
            var dailyTaskAppService = DailyTaskBuilder.Build();

            dailyTaskAppService.LiveSign();

            Assert.True(true);
        }
    }
}
