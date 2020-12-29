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
            Program.PreWorks(new string[] { });

            _scKey = Global.ConfigurationRoot["Serilog:WriteTo:6:Args:scKey"]; ;
        }

        [Fact]
        public void Test2()
        {
            ServerChanApiClient client = new ServerChanApiClient(_scKey);
            var result = client.PushMessageAsync(LogConstants.Msg).Result;
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            System.Console.ReadLine();
        }
    }
}
