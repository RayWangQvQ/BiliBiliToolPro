using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.CoolPushBatched;
using Ray.Serilog.Sinks.ServerChanBatched;
using Xunit;

namespace LogTest
{
    public class TestPushPlus
    {
        private string _scKey;

        public TestPushPlus()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });

            //_scKey = Global.ConfigurationRoot["Serilog:WriteTo:6:Args:scKey"];
        }

        [Fact]
        public void Test2()
        {
            var client = new PushPlusApiClient("");
            var result = client.PushMessage(LogConstants.Msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            System.Console.ReadLine();
        }
    }
}
