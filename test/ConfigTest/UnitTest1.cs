using System;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
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
            Program.PreWorks(new string[] { "-closeConsoleWhenEnd=1" });

            Debug.WriteLine(RayConfiguration.Root["CloseConsoleWhenEnd"]);

            string s = RayConfiguration.Root["BiliBiliCookie:UserId"];
            Debug.WriteLine(s);
            //Assert.True(!string.IsNullOrWhiteSpace(s));

            string logLevel = RayConfiguration.Root["Serilog:WriteTo:0:Args:restrictedToMinimumLevel"];
            Debug.WriteLine(logLevel);

            var options = RayContainer.Root.GetRequiredService<IOptionsMonitor<BiliBiliCookieOptions>>();

            Debug.WriteLine(JsonSerializer.Serialize(options.CurrentValue, new JsonSerializerOptions { WriteIndented = true }));
            Assert.True(!string.IsNullOrWhiteSpace(options.CurrentValue.UserId));
        }
    }
}
