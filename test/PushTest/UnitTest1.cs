using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.Push;
using Ray.BiliBiliTool.Agent.ServerChanAgent;
using Ray.BiliBiliTool.Application.Contracts;
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

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var pushService = scope.ServiceProvider.GetRequiredService<ServerChanPushService>();

                var re = pushService.Send($"测试标题{new Random().Next(100)}", "测试内容");
                Assert.True(re);
            }
        }

        [Fact]
        public void Test2()
        {
            Program.PreWorks(new string[] { });

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                Log.Logger.Debug("这是debug");
                Log.Logger.Information("这是info");
                Log.Logger.Warning("这是warning");
                Log.Logger.Fatal("这是fatal");

                var pushService = scope.ServiceProvider.GetRequiredService<IPushAppService>();
                pushService.Push();

                Assert.True(true);
            }
        }
    }
}
