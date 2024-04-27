using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

public class HomeApiTests
{
    private readonly IHomeApi _api;

    private readonly BiliCookie _ck;

    public HomeApiTests()
    {
        var envs = new List<string>
        {
            "--ENVIRONMENT=Development",
            //"HTTP_PROXY=localhost:8888",
            //"HTTPS_PROXY=localhost:8888"
        };
        IHost host = Program.CreateHost(envs.ToArray());
        _ck = host.Services.GetRequiredService<BiliCookie>();
        _api = host.Services.GetRequiredService<IHomeApi>();
    }

    [Fact]
    public async Task GetHomePageAsync_Normal_Success()
    {
        // Act
        HttpResponseMessage re = await _api.GetHomePageAsync(_ck.ToString());

        // Arrange
        var page = await re.Content.ReadAsStringAsync();

        // Assert
        re.IsSuccessStatusCode.Should().BeTrue();
        page.Should().Contain("<title>哔哩哔哩 (゜-゜)つロ 干杯~-bilibili</title>");
    }
}
