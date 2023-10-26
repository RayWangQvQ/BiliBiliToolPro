using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Ray.BiliBiliTool.Console;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace BiliAgentTest
{
    public class BiliCookieContainerTest
    {
        public BiliCookieContainerTest()
        {
            Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
        }

        [Fact]
        public async Task WebHeartBeat_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var ck = scope.ServiceProvider.GetRequiredService<BiliCookieContainer>();

            ck.SetCookies(new Uri("https://www.bilibili.com"), "innersign=0; path=/; domain=.bilibili.com");
        }
    }
}
