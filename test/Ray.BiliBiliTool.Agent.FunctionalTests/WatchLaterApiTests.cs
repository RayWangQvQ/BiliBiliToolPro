using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

[Trait("Category", "External")]
public class WatchLaterApiTests
{
    private readonly IWatchLaterApi _api;

    private readonly BiliCookie _ck;

    public WatchLaterApiTests()
    {
        var envs = new List<string>
        {
            "--ENVIRONMENT=Development",
            //"HTTP_PROXY=localhost:8888",
            //"HTTPS_PROXY=localhost:8888"
        };
        IHost host = Program.CreateHost(envs.ToArray());
        _ck = ExternalCookie.Require(host.Services);
        _api = host.Services.GetRequiredService<IWatchLaterApi>();
    }

    [Fact]
    public async Task GetListAsync_Normal_Success()
    {
        // Act
        var re = await _api.GetListAsync(5, 1, _ck.ToString());

        // Assert
        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
        re.Data!.Count.Should().BeGreaterThan(0);
        re.Data.List.Should().NotBeNull();
    }
}
