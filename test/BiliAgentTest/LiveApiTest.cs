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
            Program.CreateHost(null);
        }

        [Fact]
        [Obsolete]
        public void GetExchangeSilverStatus_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

            BiliApiResponse<ExchangeSilverStatusResponse> re = api.GetExchangeSilverStatus().Result;

            Assert.True(re.Code == 0 && re.Message == "0");
            Assert.True(re.Data.Silver >= 0);
        }

        [Fact]
        public void Silver2Coin_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var biliCookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();

            Silver2CoinRequest request = new(biliCookie.BiliJct);

            BiliApiResponse<Silver2CoinResponse> re = api.Silver2Coin(request).Result;

            Assert.True(re.Code >= 0);

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
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

            BiliApiResponse<LiveWalletStatusResponse> re = api.GetLiveWalletStatus().Result;

            Assert.True(re.Code >= 0);

            if (re.Code == 0)
            {
                Assert.True(re.Data.Silver_2_coin_left >= 0);
            }
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(re.Message));
            }
        }
    }
}
