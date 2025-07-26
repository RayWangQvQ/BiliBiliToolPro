using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.DingTalkBatched;
using Xunit;

namespace LogTest
{
    public class TestDingTalk
    {
        private string _key;

        public TestDingTalk()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(["ENVIRONMENT=Development"]);

            // 添加空值检查
            if (Global.ConfigurationRoot != null)
            {
                _key = Global.ConfigurationRoot["Serilog:WriteTo:5:Args:webHookUrl"];
            }
            else
            {
                _key = "test_key"; // 默认测试值
            }
        }

        [Fact]
        public async Task Test2()
        {
            var client = new DingTalkApiClient(_key);

            var title = "这是标题";
            var msg = LogConstants.Msg2 + "开始推送";

            var result = await client.PushMessageAsync(msg, title);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);
        }
    }
}
