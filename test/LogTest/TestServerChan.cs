using System;
using System.Diagnostics;
using System.Threading;
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
            Program.CreateHost(new string[] { });

            _scKey = Global.ConfigurationRoot["Serilog:WriteTo:6:Args:scKey"];
            _turboScKey = Global.ConfigurationRoot["Serilog:WriteTo:6:Args:turboScKey"];
        }

        [Fact]
        public void Test()
        {
            var client = new ServerChanApiClient(_scKey);

            string msg = LogConstants.Msg2;

            var result = client.PushMessage(msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            /*
             * server酱的换行有问题，一个newline换不了，要两个
             */
        }

        [Fact]
        public void TestTurbo()
        {
            var client = new ServerChanTurboApiClient(_turboScKey);

            string msg = LogConstants.Msg2;

            var result = client.PushMessage(msg, "测试");
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            /*
             * server酱的换行有问题，一个newline换不了，要两个
             */
        }
    }
}
