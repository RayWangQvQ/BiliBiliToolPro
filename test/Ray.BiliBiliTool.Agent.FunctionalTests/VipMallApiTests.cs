using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ViewMall;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

public class VipMallApiTests
{
    private readonly IVipMallApi _api;

    private readonly BiliCookie _ck;

    public VipMallApiTests()
    {
        var envs = new List<string>
        {
            "--ENVIRONMENT=Development",
            //"HTTP_PROXY=localhost:8888",
            //"HTTPS_PROXY=localhost:8888"
        };
        IHost host = Program.CreateHost(envs.ToArray());
        _ck = host.Services.GetRequiredService<BiliCookie>();
        _api = host.Services.GetRequiredService<IVipMallApi>();
    }

    [Fact]
    public async Task ViewVipMallAsync_Normal_Success()
    {
        // Arrange
        var req = new ViewVipMallRequest()
        {
            Csrf = _ck.BiliJct
        };

        // Act
        BiliApiResponse re = await _api.ViewVipMallAsync(req);

        // Assert
        re.Code.Should().Be(0);
        re.Message.Should().BeEquivalentTo("SUCCESS");
    }
}
