using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Xunit;

namespace BiliAgentTest
{
    public class VideoApiTest
    {
        public VideoApiTest()
        {
            Program.CreateHost(new[] { "--ENVIRONMENT=Development" });//默认Prd环境，这里指定为Dev后，可以读取到用户机密配置
        }

        [Fact]
        public void GetLiveWalletStatus_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory>();
            var api = scope.ServiceProvider.GetRequiredService<IVideoApi>();

            var req = new GetAlreadyDonatedCoinsRequest(248097491);
            BiliApiResponse<DonatedCoinsForVideo>? re = api.GetDonatedCoinsForVideo(req).Result;

            if (ck.Count > 0)
            {
                Assert.True(re.Code == 0 && re.Data.Multiply >= 0);
            }
            else
            {
                Assert.False(re.Code != 0);
            }
        }
    }
}
