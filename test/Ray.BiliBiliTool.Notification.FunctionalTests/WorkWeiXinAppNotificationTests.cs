using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.WorkWeiXinAppBatched;
using Xunit;

namespace Ray.BiliBiliTool.Notification.FunctionalTests
{
    [Trait("Category", "External")]
    public class WorkWeiXinAppNotificationTests
    {
        private string _agentId;
        private string _secret;
        private string _corpId;
        private string _toUser;

        public WorkWeiXinAppNotificationTests()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { "ENVIRONMENT=Development" });

            _agentId = Global.ConfigurationRoot["Serilog:WriteTo:11:Args:agentId"];
            _secret = Global.ConfigurationRoot["Serilog:WriteTo:11:Args:secret"];
            _corpId = Global.ConfigurationRoot["Serilog:WriteTo:11:Args:corpId"];

            _toUser = Global.ConfigurationRoot["Serilog:WriteTo:11:Args:toUser"];
        }

        [Fact]
        public async Task PushMessageAsync_ConfiguredCredentials_ReturnsOk()
        {
            var client = new WorkWeiXinAppApiClient(_corpId, _agentId, _secret, _toUser);

            var msg = LogConstants.Msg2;

            var result = await client.PushMessageAsync(msg);
            Debug.WriteLine(await result.Content.ReadAsStringAsync());

            Assert.True(result.StatusCode == System.Net.HttpStatusCode.OK);
        }
    }
}
