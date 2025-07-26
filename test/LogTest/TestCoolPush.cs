using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.CoolPushBatched;
using Xunit;

namespace LogTest
{
    public class TestCoolPush
    {
        private string _key;

        public TestCoolPush()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { "ENVIRONMENT=Development" });

            _key = Global.ConfigurationRoot["Serilog:WriteTo:7:Args:sKey"];
        }

        [Fact]
        public async Task Test2()
        {
            if (string.IsNullOrEmpty(_key))
            {
                Debug.WriteLine("CoolPush key not configured, skipping test");
                return;
            }

            var client = new CoolPushApiClient(_key);
            string msg = LogConstants.Msg2;

            var result = await client.PushMessageAsync(msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);
        }
    }
}
