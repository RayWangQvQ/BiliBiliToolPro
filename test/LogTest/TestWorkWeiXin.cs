using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.WorkWeiXinBatched;
using Xunit;

namespace LogTest
{
    public class TestWorkWeiXin
    {
        private string _key;

        public TestWorkWeiXin()
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { "ENVIRONMENT=Development" });

            _key = Global.ConfigurationRoot["Serilog:WriteTo:4:Args:webHookUrl"];
        }

        [Fact]
        public void Test2()
        {
            var client = new WorkWeiXinApiClient(_key);

            //string msg = LogConstants.Msg;
            string msg = LogConstants.Msg2;

            var result = client.PushMessage(msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);
        }
    }
}
