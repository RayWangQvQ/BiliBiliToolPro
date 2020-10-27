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

            string s = RayConfiguration.Root["BiliBiliCookies:UserId"];
            Debug.WriteLine(s);
            Assert.True(!string.IsNullOrWhiteSpace(s));

            var options = RayContainer.Root.GetRequiredService<IOptionsMonitor<BiliBiliCookiesOptions>>();

            Debug.WriteLine(JsonSerializer.Serialize(options.CurrentValue, new JsonSerializerOptions { WriteIndented = true }));
            Assert.True(!string.IsNullOrWhiteSpace(options.CurrentValue.UserId));
        }
    }
}
