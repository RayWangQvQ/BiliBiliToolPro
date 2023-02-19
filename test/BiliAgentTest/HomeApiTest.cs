using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Helpers;
using Xunit;

namespace BiliAgentTest
{
    public class HomeApiTest
    {
        public HomeApiTest()
        {
            Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
        }

        [Fact]
        public async Task WebHeartBeat_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var ck = scope.ServiceProvider.GetRequiredService<BiliCookie>();
            var api = scope.ServiceProvider.GetRequiredService<IHomeApi>();

            var re = await api.GetHomePageAsync(ck.ToString());

            
        }
    }
}
