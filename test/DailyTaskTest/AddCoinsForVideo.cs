using System;
using System.Diagnostics;
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
        public void TestGetCanDonatedVideo()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDonateCoinDomainService>();

                var re = service.TryGetCanDonatedVideo();

                Debug.WriteLine(re.ToJson());
            }

            Assert.True(true);
        }

        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDonateCoinDomainService>();

                service.AddCoinsForVideo();
            }

            Assert.True(true);
        }

        [Fact]
        public void Test2()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDonateCoinDomainService>();

                service.DoAddCoinForVideo("627549610", 1, true);
            }

            Assert.True(true);
        }
    }
}
