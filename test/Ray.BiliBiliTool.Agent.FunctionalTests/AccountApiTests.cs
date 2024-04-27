using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

public class AccountApiTests
{
    private readonly IAccountApi _api;

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
    }

    [Fact]
    public async Task GetCoinBalance_Normal_GetCoinBalance()
    {
        // Act
        BiliApiResponse<CoinBalance> re = await _api.GetCoinBalanceAsync();

        // Arrange

        // Assert
        re.Code.Should().Be(0);
        re.Data.Money.Should().IsNotNull();
    }
}
