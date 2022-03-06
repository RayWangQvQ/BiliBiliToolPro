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
using Xunit;

namespace BiliAgentTest
{
    public class LiveApiTest
    {
        public LiveApiTest()
        {
            Program.CreateHost(new[] { "--ENVIRONMENT=Development" });//默认Prd环境，这里指定为Dev后，可以读取到用户机密配置
        }

        [Fact]
        [Obsolete]
        public void GetExchangeSilverStatus_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory>();

            BiliApiResponse<ExchangeSilverStatusResponse> re = api.GetExchangeSilverStatus().Result;

            if (ck.Count > 0)
            {
                Assert.True(re.Code == 0 && re.Message == "0");
                Assert.True(re.Data.Silver >= 0);
            }
            else
            {
                Assert.False(re.Code != 0);
            }
        }

        [Fact]
        public void Silver2Coin_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory>();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var biliCookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();

            Silver2CoinRequest request = new(biliCookie.BiliJct);

            BiliApiResponse<Silver2CoinResponse> re = api.Silver2Coin(request).Result;

            if (re.Code == 0)
            {
                Assert.True(re.Data.Coin == 1);
            }
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(re.Message));
            }
        }

        [Fact]
        public void GetLiveWalletStatus_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory>();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

            BiliApiResponse<LiveWalletStatusResponse> re = api.GetLiveWalletStatus().Result;

            if (ck.Count > 0)
            {
                Assert.True(re.Code == 0 && re.Data.Silver_2_coin_left >= 0);
            }
            else
            {
                Assert.False(re.Code != 0);
            }
        }
    }
}
