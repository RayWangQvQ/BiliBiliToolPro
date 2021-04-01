using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.CoolPushBatched;
using Ray.Serilog.Sinks.PushPlusBatched;
using Ray.Serilog.Sinks.ServerChanBatched;
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
            Program.CreateHost(new string[] { });

            _token = Global.ConfigurationRoot["Serilog:WriteTo:9:Args:token"];
            _channel = Global.ConfigurationRoot["Serilog:WriteTo:9:Args:channel"];
            _webhook = Global.ConfigurationRoot["Serilog:WriteTo:9:Args:webhook"];
        }

        [Fact]
        public void Test2()
        {
            var client = new PushPlusApiClient(
                _token,
                channel: _channel,
                webhook: _webhook);

            var msg = LogConstants.Msg2;

            var result = client.PushMessage(msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            System.Console.ReadLine();
        }
    }
}
