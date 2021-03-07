using System;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace DailyTaskTest
{
    public class LiveTianXuan
    {
        public LiveTianXuan()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });
        }

        [Fact]
        public void GetAreaList()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

                var re = api.GetAreaList().Result;

                Assert.True(true);
            }
        }

        [Fact]
        public void GetLiveList()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

                var re = api.GetList(2, 1).Result;

                Assert.True(true);
            }
        }
    }
}
