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

            string logLevel = RayConfiguration.Root["Serilog:WriteTo:0:Args:restrictedToMinimumLevel"];
            Debug.WriteLine(logLevel);

            var options = RayContainer.Root.GetRequiredService<IOptionsMonitor<BiliBiliCookieOptions>>();

            Debug.WriteLine(JsonSerializer.Serialize(options.CurrentValue, new JsonSerializerOptions { WriteIndented = true }));
            Assert.True(!string.IsNullOrWhiteSpace(options.CurrentValue.UserId));
        }

        [Fact]
        public void LoadPrefixConfigByEnvWithNoError()
        {
            Environment.SetEnvironmentVariable("Ray_BiliBiliCookie", "UserId: 123");
            Program.PreWorks(new string[] { "-closeConsoleWhenEnd=1" });

            string result = RayConfiguration.Root["BiliBiliCookie"];

            Assert.Equal("UserId: 123", result);
            Environment.SetEnvironmentVariable("Ray_BiliBiliCookie", null);
        }

        [Fact]
        public void LoadPrefixConfigByEnvWhenValueIsNullWithNoError2()
        {
            Environment.SetEnvironmentVariable("Ray_BiliBiliCookie", null);
            Program.PreWorks(new string[] { "-closeConsoleWhenEnd=1" });

            string result = RayConfiguration.Root["BiliBiliCookie"];

            Assert.Null(result);
        }

        [Fact]
        public void CoverConfigByEnvWithNoError()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
            Program.PreWorks(new string[] { "-closeConsoleWhenEnd=1" });

            string result = RayConfiguration.Root["IsPrd"];

            Assert.Equal("True", result);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }

        /// <summary>
        /// 为配置手动赋值
        /// </summary>
        [Fact]
        public void TestSetConfiguration()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.PreWorks(new string[] { });

            var options = RayContainer.Root.GetRequiredService<IOptionsMonitor<BiliBiliCookieOptions>>();
            Debug.WriteLine(options.CurrentValue.ToJson());

            //手动赋值
            //RayConfiguration.Root["BiliBiliCookie:UserId"] = "123456";
            options.CurrentValue.SetUserId("123456");

            Debug.WriteLine($"从Configuration读取：{RayConfiguration.Root["BiliBiliCookie:UserId"]}");

            Debug.WriteLine($"从老options读取：{options.CurrentValue.ToJson()}");

            var optionsNew = RayContainer.Root.GetRequiredService<IOptionsMonitor<BiliBiliCookieOptions>>();
            Debug.WriteLine($"从新options读取：{optionsNew.CurrentValue.ToJson()}");
        }
    }
}
