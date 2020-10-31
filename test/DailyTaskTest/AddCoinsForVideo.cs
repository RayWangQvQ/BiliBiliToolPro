using System;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace DailyTaskTest
{
    public class AddCoinsForVideo
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var dailyTaskAppService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();

                dailyTaskAppService.AddCoinsForVideo();
            }

            Assert.True(true);
        }

        [Fact]
        public void Test2()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var dailyTaskAppService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();

                dailyTaskAppService.AddCoinsForVideo("627549610", 1, true);
            }

            Assert.True(true);
        }
    }
}
