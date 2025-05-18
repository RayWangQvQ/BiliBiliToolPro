using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Video;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

public class WbiServiceTest
{
    private readonly IWbiService _target;

    public WbiServiceTest()
    {
        var envs = new List<string>
        {
            "--ENVIRONMENT=Development",
            //"HTTP_PROXY=localhost:8888",
            //"HTTPS_PROXY=localhost:8888"
        };
        IHost host = Program.CreateHost(envs.ToArray());
        _target = host.Services.GetRequiredService<IWbiService>();
    }

    [Fact]
    public async void SetWridAsync_SendRequest_SetWridSuccess()
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
        await _target.SetWridAsync(req, null);

        // Assert
        req.w_rid.Should().NotBeNullOrWhiteSpace();
        req.wts.Should().NotBe(0);
    }

    [Fact]
    public void EncWbi_InputParams_GetCorrectWbiResult()
    {
        // Arrange
        var wbiDto = new WbiImg()
        {
            img_url = "https://i0.hdslb.com/bfs/wbi/653657f524a547ac981ded72ea172057.png",
            sub_url = "https://i0.hdslb.com/bfs/wbi/6e4909c702f846728e64f6007736a338.png",
        };
        var dic = new Dictionary<string, string>()
        {
            { "foo", "114" },
            { "bar", "514" },
            { "baz", "1919810" },
        };
        var timeSpan = 1684746387;
        var expectResult = "d3cbd2a2316089117134038bf4caf442";

        // Act
        var re = _target.EncWbi(dic, wbiDto.ImgKey, wbiDto.SubKey, timeSpan);

        // Assert
        re.w_rid.Should().BeEquivalentTo(expectResult);
        re.wts.Should().Be(timeSpan);
    }
}
