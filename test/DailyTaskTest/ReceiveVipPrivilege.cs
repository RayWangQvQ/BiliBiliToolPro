using System;
using DailyTaskTest.Share;
using Xunit;

namespace DailyTaskTest
{
    public class ReceiveVipPrivilege
    {
        [Fact]
        public void Test1()
        {
            var dailyTaskAppService = DailyTaskBuilder.Build();

            dailyTaskAppService.Login();

            dailyTaskAppService.ReceiveVipPrivilege();

            Assert.True(true);
        }
    }
}
