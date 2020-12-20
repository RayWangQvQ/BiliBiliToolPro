using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.Push;
using Ray.BiliBiliTool.Agent.Push.WorkWeiXinAgent.Dtos;
using Ray.BiliBiliTool.Agent.ServerChanAgent;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Serilog;
using Xunit;

namespace PushTest
{
    public class WorkWeiXin
    {
        private string _url = "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=1d919c89-5c17-4970-91ac-82f63a672700";

        private WorkWeiXinPushRequest _requestDto = new WorkWeiXinPushRequest
        {
            Msgtype = "markdown",
            Markdown = new Markdown { Content = "123" }
        };


        [Fact]
        public void Push1()
        {
            Program.PreWorks(null);

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                Log.Logger.Debug("这是debug");
                Log.Logger.Information("这是info");
                Log.Logger.Warning("这是warning");
                Log.Logger.Fatal("这是fatal");

                var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                HttpClient client = factory.CreateClient();

                var todoItemJson = new StringContent(
                    JsonSerializer.Serialize(_requestDto, JsonSerializerOptionsBuilder.DefaultOptions),
                    Encoding.UTF8,
                    "application/json");
                var re = client.PostAsync(_url, todoItemJson).Result;
                var reContent = re.Content.ReadAsStringAsync().Result;
            }
        }

        [Fact]
        public void WorkWeiXinPush()
        {
            Program.PreWorks(null);

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                Log.Logger.Debug("这是debug");
                Log.Logger.Information("这是info");
                Log.Logger.Warning("这是warning");
                Log.Logger.Fatal("这是fatal");

                var pushServices = scope.ServiceProvider.GetRequiredService<IEnumerable<IPushService>>();
                var pushService = pushServices.First(x => x.Name == "WorkWeiXin");
                var re = pushService.Send("title", "content");
            }
            System.Console.ReadLine();
        }
    }
}
