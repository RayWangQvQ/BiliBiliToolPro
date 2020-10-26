using System;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Console;
using Xunit;
using Ray.BiliBiliTool.Infrastructure;

namespace ConfigTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            string s = RayConfiguration.Root["DailyTaskConfig:NumberOfCoins"];
            Debug.WriteLine(s);
            Assert.Equal("5", s);

            var options = RayContainer.Root.GetRequiredService<IOptionsMonitor<DailyTaskOptions>>();

            Debug.WriteLine(JsonSerializer.Serialize(options.CurrentValue, new JsonSerializerOptions { WriteIndented = true }));
            Assert.Equal(5, options.CurrentValue.NumberOfCoins);
        }
    }
}
