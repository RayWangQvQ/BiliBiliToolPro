using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Xunit;
using System.Text.Json;
using Ray.BiliBiliTool.Infrastructure;

namespace LoginTest
{
    public class Login
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var dailyTask = scope.ServiceProvider.GetRequiredService<IAccountDomainService>();

                var userInfo = dailyTask.LoginByCookie();
                Debug.WriteLine(JsonSerializer.Serialize(userInfo));

                Assert.NotNull("");
            }
        }
    }
}
