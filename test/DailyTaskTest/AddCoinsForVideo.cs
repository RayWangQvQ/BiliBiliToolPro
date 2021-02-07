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
    public class AddCoinsForVideo
    {
        public AddCoinsForVideo()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });
        }

        [Fact]
        public void TestGetCanDonatedVideo()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
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
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDonateCoinDomainService>();

                service.AddCoinsForVideos();
            }

            Assert.True(true);
        }

        [Fact]
        public void Test2()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDonateCoinDomainService>();

                service.DoAddCoinForVideo("543318157", 1, true);
            }

            Assert.True(true);
        }

        [Fact]
        public void GetVideoInfo()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IVideoApi>();

                //var re = service.GetVideoDetail("246364184").Result;//×ÔÖÆ
                var re = service.GetVideoDetail("373987080").Result;//×ªÔØ

            }

            Assert.True(true);
        }
    }
}
