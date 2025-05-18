using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Xunit;

namespace BiliAgentTest
{
    public class LiveApiTest
    {
        public LiveApiTest()
        {
            Program.CreateHost(new[] { "--ENVIRONMENT=Development" }); //Ä¬ÈÏPrd»·¾³£¬ÕâÀïÖ¸¶¨ÎªDevºó£¬¿ÉÒÔ¶ÁÈ¡µ½ÓÃ»§»úÃÜÅäÖÃ
        }

        [Fact]
        [Obsolete]
        public void GetExchangeSilverStatus_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory<BiliCookie>>();

            BiliApiResponse<ExchangeSilverStatusResponse> re = api.GetExchangeSilverStatus(
                null
            ).Result;

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

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory<BiliCookie>>();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var biliCookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();

            Silver2CoinRequest request = new(biliCookie.BiliJct);

            BiliApiResponse<Silver2CoinResponse> re = api.Silver2Coin(request, null).Result;

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

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory<BiliCookie>>();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

            BiliApiResponse<LiveWalletStatusResponse> re = api.GetLiveWalletStatus(null).Result;

            if (ck.Count > 0)
            {
                Assert.True(re.Code == 0 && re.Data.Silver_2_coin_left >= 0);
            }
            else
            {
                Assert.False(re.Code != 0);
            }
        }

        [Fact]
        public void GetMedalWall_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory<BiliCookie>>();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

            BiliApiResponse<MedalWallResponse> re = api.GetMedalWall("919174", null).Result;

            Assert.NotEmpty(re.Data.List);

            var md = re.Data.List[0];
            Assert.NotNull(md);
            Assert.False(String.IsNullOrEmpty(md.Link));
            Assert.False(String.IsNullOrEmpty(md.Target_name));
            Assert.NotNull(md.Medal_info);
            Assert.False(String.IsNullOrEmpty(md.Medal_info.Medal_name));
            Assert.True(md.Medal_info.Medal_id > 0);
        }

        [Fact]
        public void WearMedalWall_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory<BiliCookie>>();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var biliCookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();

            // 猫雷粉丝牌
            var request = new WearMedalWallRequest(biliCookie.BiliJct, 365421); //todo

            BiliApiResponse re = api.WearMedalWall(request, null).Result;

            Assert.True(re.Code == 0);
            re.Code.Should().BeOneOf(0, 1500005);
        }

        [Fact]
        public async Task GetSpaceInfo_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory<BiliCookie>>();
            var api = scope.ServiceProvider.GetRequiredService<IUpInfoApi>();

            var wbiService = scope.ServiceProvider.GetRequiredService<IWbiService>();

            var req = new GetSpaceInfoDto() { mid = 919174L };

            BiliApiResponse<GetSpaceInfoResponse> re = api.GetSpaceInfo(
                req,
                ck.GetCookie(0).ToString()
            ).Result;

            Assert.True(re.Code == 0);
            Assert.NotNull(re.Data);
            Assert.Equal(919174, re.Data.Mid);
            Assert.NotNull(re.Data.Live_room);
            Assert.Equal(3115258, re.Data.Live_room.Roomid);
            Assert.False(String.IsNullOrEmpty(re.Data.Name));
            Assert.False(String.IsNullOrEmpty(re.Data.Live_room.Title));
        }

        [Fact]
        public void SendLiveDanmuku_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory<BiliCookie>>();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var biliCookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();

            var request = new SendLiveDanmukuRequest(biliCookie.BiliJct, 63666, "63666");

            BiliApiResponse re = api.SendLiveDanmuku(request, null).Result;

            Assert.True(re.Code == 0);
        }
    }
}
