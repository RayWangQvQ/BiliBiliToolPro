using System;
using System.Diagnostics;
using System.Threading.Tasks;
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
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { "ENVIRONMENT=Development" });

            _key = Global.ConfigurationRoot["Serilog:WriteTo:4:Args:webHookUrl"];
        }

        [Fact]
        public async Task Test2()
        {
            var client = new WorkWeiXinApiClient(_key, WorkWeiXinMsgType.text);

            //string msg = LogConstants.Msg;
            string msg = LogConstants.Msg2;

            var result = await client.PushMessageAsync(msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);
        }
    }
}
