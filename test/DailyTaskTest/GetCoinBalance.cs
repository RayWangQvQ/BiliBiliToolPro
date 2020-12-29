using System;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace DailyTaskTest
{
    public class GetCoinBalance
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var dailyTaskAppService = scope.ServiceProvider.GetRequiredService<ICoinDomainService>();

                var number = dailyTaskAppService.GetCoinBalance();
                Assert.True(number >= 0);
            }
        }
    }
}
