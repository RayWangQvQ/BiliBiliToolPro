using System;
using DailyTaskTest.Share;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Xunit;

namespace DailyTaskTest
{
    public class LiveSign
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = Program.ServiceProviderRoot.CreateScope())
            {
                var dailyTask = scope.ServiceProvider.GetRequiredService<ILiveDomainService>();

                dailyTask.LiveSign();

                Assert.True(true);
            }
        }
    }
}
