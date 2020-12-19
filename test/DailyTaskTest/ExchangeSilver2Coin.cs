using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace DailyTaskTest
{
    public class ExchangeSilver2Coin
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var dailyTask = scope.ServiceProvider.GetRequiredService<ILiveDomainService>();

                dailyTask.ExchangeSilver2Coin();

                Assert.True(true);
            }
        }

        [Fact]
        public void Test2()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.PreWorks(new string[] { });

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

                var queryStatus = api.GetExchangeSilverStatus().Result;
                Debug.WriteLine(queryStatus.ToJson());

                Assert.True(true);
            }
        }
    }
}
