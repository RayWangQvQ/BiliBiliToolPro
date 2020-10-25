using System;
using DailyTaskTest.Share;
using Xunit;

namespace DailyTaskTest
{
    public class AddCoinsForVideo
    {
        [Fact]
        public void Test1()
        {
            var dailyTaskAppService = DailyTaskBuilder.Build();

            bool b = dailyTaskAppService.AddCoinsForVideo("585105826", 1, 0);

            Assert.True(true);
        }
    }
}
