using System.Text.Json;
using System.Diagnostics;
using Xunit;
using DailyTaskTest.Share;
using Ray.BiliBiliTool.Console;

namespace GetDailyTaskStatusTest
{
    public class GetDailyTaskStatus
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            var dailyTask = DailyTaskBuilder.Build();
            var re = dailyTask.GetDailyTaskStatus();

            Debug.WriteLine(JsonSerializer.Serialize(re, new JsonSerializerOptions { WriteIndented = true }));

            Assert.NotNull(re);
        }
    }
}
