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
            Program.PreWorks(new BiliBiliTool.Login.Verify("220893216",
                "41bd553c%2C1612573459%2Ca8673*81",
                "b85b804da65ef6454a153e7606d8d967"));

            DailyTask dailyTask = Program.ServiceProviderRoot.GetRequiredService<DailyTask>();
            var re = dailyTask.GetDailyTaskStatus();

            Debug.WriteLine(JsonSerializer.Serialize(re, new JsonSerializerOptions { WriteIndented = true }));

            Assert.NotNull(re);
        }
    }
}
