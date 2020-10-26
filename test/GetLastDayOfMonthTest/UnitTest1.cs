using System;
using System.Diagnostics;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure.Extensions;
using Xunit;

namespace GetLastDayOfMonthTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            var dateTime = DateTime.Now.LastDayOfMonth();
            Debug.WriteLine(dateTime);

            Assert.True(true);
        }
    }
}
