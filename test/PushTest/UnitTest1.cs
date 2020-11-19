using System;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.ServerChanAgent;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Serilog;
using Xunit;

namespace PushTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var pushService = scope.ServiceProvider.GetRequiredService<PushService>();

                var re = pushService.DoSend($"测试标题{new Random().Next(100)}", "测试内容");
                Assert.True(re.Errno == 0);
            }
        }

        [Fact]
        public void Test2()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                Log.Logger.Debug("����debug");
                Log.Logger.Information("����info");
                Log.Logger.Warning("����warning");
                Log.Logger.Fatal("����fatal");

                var pushService = scope.ServiceProvider.GetRequiredService<PushService>();
                var re = pushService.SendStringWriter();

                Assert.True(re.Errno == 0);
            }
        }
    }
}
