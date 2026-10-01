using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.UpInfo;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.FunctionalTests;
using Ray.BiliBiliTool.Console;
using Xunit;

namespace BiliAgentTest
{
    [Trait("Category", "External")]
    public class LiveApiTests : IDisposable
    {
        private readonly IHost _host;

        public LiveApiTests()
        {
            _host = Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
        }

        [Fact]
        [Obsolete]
        public async Task GetExchangeSilverStatus_Normal_Success()
        {
            using var scope = _host.Services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var cookie = RequireCookie(scope.ServiceProvider);

            BiliApiResponse<ExchangeSilverStatusResponse> re = await api.GetExchangeSilverStatus(
                cookie
            );

            Assert.Equal(0, re.Code);
            Assert.Equal("0", re.Message);
            Assert.NotNull(re.Data);
            Assert.True(re.Data.Silver >= 0);
        }

        [Fact]
        public async Task Silver2Coin_Normal_Success()
        {
            using var scope = _host.Services.CreateScope();

            var cookie = RequireCookie(scope.ServiceProvider);
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var biliCookie = ExternalCookie.Require(scope.ServiceProvider);

            Silver2CoinRequest request = new(biliCookie.BiliJct);

            BiliApiResponse<Silver2CoinResponse> re = await api.Silver2Coin(request, cookie);

            if (re.Code == 0)
            {
                Assert.True(re.Data!.Coin == 1);
            }
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(re.Message));
            }
        }

        [Fact]
        public async Task GetLiveWalletStatus_Normal_Success()
        {
            using var scope = _host.Services.CreateScope();

            var cookie = RequireCookie(scope.ServiceProvider);
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

            BiliApiResponse<LiveWalletStatusResponse> re = await api.GetLiveWalletStatus(cookie);

            Assert.Equal(0, re.Code);
            Assert.NotNull(re.Data);
            Assert.True(re.Data.Silver_2_coin_left >= 0);
        }

        [Fact]
        public async Task GetMedalWall_Normal_Success()
        {
            using var scope = _host.Services.CreateScope();

            var cookie = RequireCookie(scope.ServiceProvider);
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

            BiliApiResponse<MedalWallResponse> re = await api.GetMedalWall("919174", cookie);

            Assert.Equal(0, re.Code);
            Assert.NotNull(re.Data);
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
        public async Task WearMedalWall_Normal_Success()
        {
            using var scope = _host.Services.CreateScope();

            var cookie = RequireCookie(scope.ServiceProvider);
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var biliCookie = ExternalCookie.Require(scope.ServiceProvider);

            // 猫雷粉丝牌
            var request = new WearMedalWallRequest(biliCookie.BiliJct, 365421); //todo

            BiliApiResponse re = await api.WearMedalWall(request, cookie);

            re.Code.Should().BeOneOf(0, 1500005);
        }

        [Fact]
        public async Task GetSpaceInfo_Normal_Success()
        {
            using var scope = _host.Services.CreateScope();

            var cookie = RequireCookie(scope.ServiceProvider);
            var api = scope.ServiceProvider.GetRequiredService<IApiApi>();

            var req = new GetSpaceInfoDto() { mid = 919174L };

            BiliApiResponse<GetSpaceInfoResponse> re = await api.GetSpaceInfo(req, cookie);

            Assert.True(re.Code == 0);
            Assert.NotNull(re.Data);
            Assert.Equal(919174, re.Data.Mid);
            Assert.NotNull(re.Data.Live_room);
            Assert.Equal(3115258, re.Data.Live_room.Roomid);
            Assert.False(String.IsNullOrEmpty(re.Data.Name));
            Assert.False(String.IsNullOrEmpty(re.Data.Live_room.Title));
        }

        [Fact]
        public async Task SendLiveDanmuku_Normal_Success()
        {
            using var scope = _host.Services.CreateScope();

            var cookie = RequireCookie(scope.ServiceProvider);
            var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
            var biliCookie = ExternalCookie.Require(scope.ServiceProvider);

            var request = new SendLiveDanmukuRequest(biliCookie.BiliJct, 63666, "63666");

            BiliApiResponse re = await api.SendLiveDanmuku(request, cookie);

            Assert.True(re.Code == 0);
        }

        private static string RequireCookie(IServiceProvider services)
        {
            return ExternalCookie.Require(services).ToString();
        }

        public void Dispose() => _host.Dispose();
    }
}
