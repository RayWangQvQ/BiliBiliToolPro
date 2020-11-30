using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Xunit;
using System.Text.Json;
using Ray.BiliBiliTool.Infrastructure;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

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

        [Fact]
        public void Test2()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                var request = new HttpRequestMessage(HttpMethod.Get,
                    "https://api.bilibili.com/x/web-interface/nav");

                var cookie = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<BiliBiliCookieOptions>>().CurrentValue.ToString();
                request.Headers.Add("Cookie", cookie);
                request.Headers.Add("Accept", "application/json, text/plain, */*");
                request.Headers.Add("Connection", "keep-alive");
                //request.Headers.Add("Referer", "https://www.bilibili.com/");//º”¡Àª·412

                var client = httpClientFactory.CreateClient();

                var response = client.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    var responseStr = response.Content.ReadAsStringAsync().Result;
                    Debug.WriteLine(responseStr);
                }
                else
                {

                }

                Assert.NotNull("");
            }
        }
    }
}
