using System;
using System.Diagnostics;
using BiliBiliTool;
using BiliBiliTool.Task;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GetLastDayOfMonthTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new BiliBiliTool.Login.Verify("", "", ""));

            var task = Program.ServiceProviderRoot.GetRequiredService<DailyTask>();

            var dateTime = task.GetLastDayOfMonth(DateTime.Now);
            Debug.WriteLine(dateTime);

            Assert.True(true);
        }
    }
}
