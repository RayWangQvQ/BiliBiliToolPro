using System;
using System.Text.Json;
using System.Diagnostics;
using BiliBiliTool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Console;
using Xunit;

namespace ConfigTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            string s = Program.ConfigurationRoot["DailyTaskConfig:NumberOfCoins"];
            Debug.WriteLine(s);
            Assert.Equal("5", s);

            var options = Program.ServiceProviderRoot.GetRequiredService<IOptionsMonitor<DailyTaskOptions>>();

            Debug.WriteLine(JsonSerializer.Serialize(options.CurrentValue, new JsonSerializerOptions { WriteIndented = true }));
            Assert.Equal(5, options.CurrentValue.NumberOfCoins);
        }
    }
}
