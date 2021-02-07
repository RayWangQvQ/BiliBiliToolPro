using System.Text.Json;
using System.Diagnostics;
using Xunit;
using Ray.BiliBiliTool.Console;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using System;

namespace GetDailyTaskStatusTest
{
    public class GetDailyTaskStatus
    {
        public GetDailyTaskStatus()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });
        }

        [Fact]
        public void Test1()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var dailyTaskService = scope.ServiceProvider.GetRequiredService<IAccountDomainService>();

                var re = dailyTaskService.GetDailyTaskStatus();

                Debug.WriteLine(JsonSerializer.Serialize(re, new JsonSerializerOptions { WriteIndented = true }));

                Assert.NotNull(re);
            }
        }
    }
}
