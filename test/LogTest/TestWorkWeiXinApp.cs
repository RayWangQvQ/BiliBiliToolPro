using System;
using System.Diagnostics;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.WorkWeiXinAppBatched;
using Xunit;

namespace LogTest
{
    public class TestWorkWeiXinApp
    {
        private string _agentId;
        private string _secret;
        private string _corpId;
        private string _toUser;

        public TestWorkWeiXinApp()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });

            _agentId = Global.ConfigurationRoot["Serilog:WriteTo:11:Args:agentId"];
            _secret = Global.ConfigurationRoot["Serilog:WriteTo:11:Args:secret"];
            _corpId = Global.ConfigurationRoot["Serilog:WriteTo:11:Args:corpId"];

            _toUser = Global.ConfigurationRoot["Serilog:WriteTo:11:Args:toUser"];
        }

        [Fact]
        public void Test()
        {
            var client = new WorkWeiXinAppApiClient(_corpId,_agentId, _secret, _toUser);

            var msg = LogConstants.Msg2;

            var result = client.PushMessage(msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            Assert.True(result.StatusCode == System.Net.HttpStatusCode.OK);
        }
    }
}
