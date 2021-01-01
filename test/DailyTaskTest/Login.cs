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
using System;

namespace LoginTest
{
    public class Login
    {
        public Login()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.PreWorks(new string[] { });
        }

        [Fact]
        public void Test1()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
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

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                var request = new HttpRequestMessage(HttpMethod.Get,
                    "https://api.bilibili.com/x/web-interface/nav");

                var cookie = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<BiliBiliCookieOptions>>().CurrentValue.ToString();
                request.Headers.Add("Cookie", cookie);
                //request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
                ////request.Headers.Add("accept-encoding", "gzip, deflate, br");
                ////request.Headers.Add("accept-language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
                //request.Headers.Add("upgrade-insecure-requests", "1");
                //request.Headers.Add("sec-fetch-dest", "document");
                //request.Headers.Add("sec-fetch-mode", "navigate");
                //request.Headers.Add("sec-fetch-site", "none");
                //request.Headers.Add("sec-fetch-user", "?1");
                //request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36 Edg/86.0.622.69");

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
