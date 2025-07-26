using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.ServerChanBatched;
using Xunit;

namespace LogTest
{
    public class TestServerChan
    {
        private string _scKey;
        private string _turboScKey;

        public TestServerChan()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { "ENVIRONMENT=Development" });

            _scKey = Global.ConfigurationRoot["Serilog:WriteTo:6:Args:scKey"];
            _turboScKey = Global.ConfigurationRoot["Serilog:WriteTo:6:Args:turboScKey"];
        }

        [Fact]
        public async Task Test()
        {
            var client = new ServerChanApiClient(_scKey);

            string msg = LogConstants.Msg2;

            var result = await client.PushMessageAsync(msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            /*
             * server酱的换行有问题，一个newline换不了，要两个
             */
        }

        [Fact]
        public async Task TestTurbo()
        {
            var client = new ServerChanTurboApiClient(_turboScKey);

            string msg = LogConstants.Msg2;

            var result = await client.PushMessageAsync(msg, "测试");
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            /*
             * server酱的换行有问题，一个newline换不了，要两个
             */
        }
    }
}
