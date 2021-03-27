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

        public TestServerChan()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });

            _scKey = Global.ConfigurationRoot["Serilog:WriteTo:6:Args:scKey"]; ;
        }

        [Fact]
        public void Test2()
        {
            var client = new ServerChanApiClient(_scKey);

            string msg = LogConstants.Msg2;

            var result = client.PushMessage(msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            System.Console.ReadLine();

            /*
             * server酱的换行有问题，一个newline换不了，要两个
             */
        }
    }
}
