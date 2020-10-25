using System;
using System.Diagnostics;
using BiliBiliTool;
using BiliBiliTool.Task;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console.Extensions;
using Xunit;

namespace GetLastDayOfMonthTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            var task = Program.ServiceProviderRoot.GetRequiredService<DailyTask>();

            var dateTime = DateTime.Now.LastDayOfMonth();
            Debug.WriteLine(dateTime);

            Assert.True(true);
        }
    }
}
