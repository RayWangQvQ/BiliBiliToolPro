using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Xunit;

namespace BiliAgentTest
{
    public class LiveTraceApiTest
    {
        public LiveTraceApiTest()
        {
            Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
        }

        [Fact]
        public void WebHeartBeat_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory>();
            var api = scope.ServiceProvider.GetRequiredService<ILiveTraceApi>();

            var request = new WebHeartBeatRequest(63666, 60);

            var re = api.WebHeartBeat(request).Result;

            Assert.Equal(0, re.Code);
            Assert.Equal("0", re.Message);
            Assert.Equal(60, re.Data.Next_interval);
        }
    }
}
