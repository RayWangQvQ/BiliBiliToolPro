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
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace BiliAgentTest
{
    public class LiveApiTest
    {
        public LiveApiTest()
        {
            Program.CreateHost(new[] { "--ENVIRONMENT=Development" });//Ä¬ÈÏPrd»·¾³£¬ÕâÀïÖ¸¶¨ÎªDevºó£¬¿ÉÒÔ¶ÁÈ¡µ½ÓÃ»§»úÃÜÅäÖÃ
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

        [Fact]
        public void GetMedalWall_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory>();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

            BiliApiResponse<MedalWallResponse> re = api.GetMedalWall("919174").Result;

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

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory>();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var biliCookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();

            // 猫雷粉丝牌
            var request = new WearMedalWallRequest(biliCookie.BiliJct, 365421);

            BiliApiResponse re = api.WearMedalWall(request).Result;

            Assert.True(re.Code == 0);
        }

        [Fact]
        public async Task GetSpaceInfo_Normal_Success()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory>();
            var api = scope.ServiceProvider.GetRequiredService<IUserInfoApi>();
            var biliCookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();

            var domainService = scope.ServiceProvider.GetRequiredService<IWbiDomainService>();

            var req = new GetSpaceInfoDto()
            {
                mid = 919174L
            };

            var w_ridDto = await domainService.GetWridAsync(req);

            var fullDto = new GetSpaceInfoFullDto()
            {
                mid = 919174L,
                w_rid = w_ridDto.w_rid,
                wts = w_ridDto.wts
            };



            BiliApiResponse<GetSpaceInfoResponse> re = api.GetSpaceInfo(fullDto).Result;

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

            var ck = scope.ServiceProvider.GetRequiredService<CookieStrFactory>();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var biliCookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();

            var request = new SendLiveDanmukuRequest(biliCookie.BiliJct, 63666, "63666");

            BiliApiResponse re = api.SendLiveDanmuku(request).Result;

            Assert.True(re.Code == 0);
        }
    }
}
