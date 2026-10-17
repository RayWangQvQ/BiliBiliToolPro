using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.AccountApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

[Trait("Category", "External")]
public class AccountApiTests
{
    private readonly IAccountApi _api;
    private readonly BiliCookie _ck;

    public AccountApiTests()
    {
        var envs = new List<string>
        {
            "--ENVIRONMENT=Development",
            //"HTTP_PROXY=localhost:8888",
            //"HTTPS_PROXY=localhost:8888"
        };
        IHost host = Program.CreateHost(envs.ToArray());
        _api = host.Services.GetRequiredService<IAccountApi>();
        _ck = ExternalCookie.Require(host.Services);
    }

    [Fact]
    public async Task GetCoinBalance_Normal_GetCoinBalance()
    {
        // Act
        BiliApiResponse<CoinBalance> re = await _api.GetCoinBalanceAsync(_ck.ToString());

        // Arrange

        // Assert
        re.Code.Should().Be(0);
        re.Data!.Money.Should().NotBeNull();
    }
}
