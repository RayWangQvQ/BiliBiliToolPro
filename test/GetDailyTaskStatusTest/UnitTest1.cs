using System;
using System.Text.Json;
using System.Diagnostics;
using BiliBiliTool;
using BiliBiliTool.Task;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GetDailyTaskStatusTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            DailyTask dailyTask = Program.ServiceProviderRoot.GetRequiredService<DailyTask>();
            var re = dailyTask.GetDailyTaskStatus();

            Debug.WriteLine(JsonSerializer.Serialize(re, new JsonSerializerOptions { WriteIndented = true }));

            Assert.NotNull(re);
        }
    }
}
