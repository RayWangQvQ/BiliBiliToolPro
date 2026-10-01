using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Coin;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;
using Xunit;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

[Trait("Category", "External")]
public class VideoApiTests : IDisposable
{
    private readonly IHost _host;

    public VideoApiTests()
    {
        _host = Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
    }

    [Fact]
    public async Task GetDonatedCoinsForVideo_ValidVideo_ReportsCoinCount()
    {
        using var scope = _host.Services.CreateScope();

        var ck = ExternalCookie.Require(scope.ServiceProvider);
        var api = scope.ServiceProvider.GetRequiredService<IApiApi>();

        var req = new GetAlreadyDonatedCoinsRequest(248097491);
        BiliApiResponse<DonatedCoinsForVideo>? re = await api.GetDonatedCoinsForVideo(
            req,
            ck.ToString()
        );

        Assert.Equal(0, re.Code);
        Assert.NotNull(re.Data);
        Assert.True(re.Data.Multiply >= 0);
    }

    [Fact]
    public async Task GetBangumiBySsid_ValidSsid_ReturnsSuccess()
    {
        using var scope = _host.Services.CreateScope();

        var api = scope.ServiceProvider.GetRequiredService<IApiApi>();
        var req = await api.GetBangumiBySsid(46508, string.Empty);

        Assert.Equal(0, req.Code);
    }

    [Fact]
    public async Task GetRegionRankingVideosV2_ValidRequest_ReturnsSuccess()
    {
        using var scope = _host.Services.CreateScope();

        var api = scope.ServiceProvider.GetRequiredService<IApiApi>();
        var req = await api.GetRegionRankingVideosV2();

        Assert.Equal(0, req.Code);
    }

    public void Dispose() => _host.Dispose();
}
