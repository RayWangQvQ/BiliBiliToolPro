using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.PushPlusBatched;
using Xunit;

namespace LogTest
{
    public class TestPushPlus
    {
        private string _token;
        private string _channel;
        private string _webhook;

        public TestPushPlus()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { "ENVIRONMENT=Development" });

            _token = Global.ConfigurationRoot["Serilog:WriteTo:9:Args:token"];
            _channel = Global.ConfigurationRoot["Serilog:WriteTo:9:Args:channel"];
            _webhook = Global.ConfigurationRoot["Serilog:WriteTo:9:Args:webhook"];
        }

        [Fact]
        public async Task Test2()
        {
            var client = new PushPlusApiClient(_token, channel: _channel, webhook: _webhook);

            var msg = LogConstants.Msg2;

            var result = await client.PushMessageAsync(msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);
        }
    }
}
