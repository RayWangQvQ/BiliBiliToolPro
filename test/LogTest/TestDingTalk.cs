using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.DingTalkBatched;
using Ray.Serilog.Sinks.TelegramBatched;
using Ray.Serilog.Sinks.WorkWeiXinBatched;
//using Serilog;
using Xunit;

namespace LogTest
{
    public class TestDingTalk
    {
        private string _key;

        public TestDingTalk()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });

            _key = Global.ConfigurationRoot["Serilog:WriteTo:5:Args:webHookUrl"];
        }

        [Fact]
        public void Test2()
        {
            var client = new DingTalkApiClient(_key);

            var msg = LogConstants.Msg2 + "开始推送";

            var result = client.PushMessage(msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            System.Console.ReadLine();
        }
    }
}
