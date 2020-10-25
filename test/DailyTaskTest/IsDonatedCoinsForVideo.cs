using System;
using DailyTaskTest.Share;
using Xunit;

namespace DailyTaskTest
{
    public class IsDonatedCoinsForVideo
    {
        [Fact]
        public void Test1()
        {
            string aid = "585105826";

            var dailyTaskAppService = DailyTaskBuilder.Build();

            bool result = dailyTaskAppService.IsDonatedCoinsForVideo(aid);
            Assert.False(result);
        }
    }
}
