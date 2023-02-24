using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Infrastructure;

namespace AppServiceTest.DailyTask
{
    public class DonateCoinsTest
    {
        public DonateCoinsTest()
        {
            Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
        }

        [Fact]
        public void Test1()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();
            var appService = scope.ServiceProvider.GetRequiredService<IDailyTaskAppService>();


        }
    }
}
