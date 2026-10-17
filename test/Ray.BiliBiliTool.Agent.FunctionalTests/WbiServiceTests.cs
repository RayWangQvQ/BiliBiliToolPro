using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

[Trait("Category", "External")]
public class WbiServiceTests
{
    private readonly IWbiService _target;
    private readonly BiliCookie _ck;

    public WbiServiceTests()
    {
        var envs = new List<string>
        {
            "--ENVIRONMENT=Development",
            //"HTTP_PROXY=localhost:8888",
            //"HTTPS_PROXY=localhost:8888"
        };
        IHost host = Program.CreateHost(envs.ToArray());
        _target = host.Services.GetRequiredService<IWbiService>();
        _ck = ExternalCookie.Require(host.Services);
    }

    [Fact]
    public async Task SetWridAsync_SendRequest_SetWridSuccess()
    {
        // Arrange
        var upId = 1585227649;
        var req = new SearchVideosByUpIdDto()
        {
            mid = upId,
            ps = 30,
            tid = 0,
            pn = 1,
            keyword = "",
            order = "pubdate",
            platform = "web",
            web_location = 1550101,
            order_avoided = "true",
        };

        // Act
        await _target.SetWridAsync(req, _ck);

        // Assert
        req.w_rid.Should().NotBeNullOrWhiteSpace();
        req.wts.Should().NotBe(0);
    }
}
