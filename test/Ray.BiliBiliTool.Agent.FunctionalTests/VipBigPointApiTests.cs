using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint.ThreeDaysSign;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;
using Xunit.Abstractions;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

[Trait("Category", "External")]
public class VipBigPointApiTests
{
    private readonly IApiApi _api;

    private readonly ITestOutputHelper _output;
    private readonly BiliCookie _ck;

    public VipBigPointApiTests(ITestOutputHelper output)
    {
        _output = output;

        var envs = new List<string>
        {
            "--ENVIRONMENT=Development",
            //"HTTP_PROXY=localhost:8888",
            //"HTTPS_PROXY=localhost:8888"
        };
        IHost host = Program.CreateHost(envs.ToArray());
        _ck = ExternalCookie.Require(host.Services);
        _api = host.Services.GetRequiredService<IApiApi>();
    }

    [Fact]
    public async Task GetTaskListAsync_Normal_Success()
    {
        // Arrange
        // Act
        BiliApiResponse<VipBigPointCombine> re = await _api.GetCombineAsync(
            new GetCombineRequest { csrf = _ck.BiliJct, buvid = _ck.Buvid },
            _ck.ToString()
        );

        // Assert
        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
        re.Data.Task_info.Modules.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task Sign2Async_ValidAccount_ReturnsSignedStatus()
    {
        var status = await _api.GetThreeDaySignAsync(
            new ThreeDaySignRequest { csrf = _ck.BiliJct },
            _ck.ToString()
        );
        status.Code.Should().Be(0);
        status.Data.Should().NotBeNull();
        if (status.Data.three_day_sign.signed)
            return;

        var re = await _api.Sign2Async(
            new Sign2RequestPath(_ck.BiliJct),
            new Sign2Request(),
            _ck.ToString()
        );
        _output.WriteLine(re.ToJsonStr());
        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetVouchersInfoAsync_Normal_Success()
    {
        // Arrange
        // Act
        var re = await _api.GetVouchersInfoAsync(_ck.ToString());

        // Assert
        re.Code.Should().Be(0);
        re.Data!.List.Should().Contain(x => x.Type == 9);
    }

    [Fact]
    public async Task GetVipExperienceAsync_Normal_Success()
    {
        // Arrange
        var req = new VipExperienceRequest() { csrf = _ck.BiliJct };

        // Act
        BiliApiResponse re = await _api.ObtainVipExperienceAsync(req, _ck.ToString());

        // Assert
        re.Code.Should()
            .BeOneOf(
                new List<int>
                {
                    0,
                    6034005, //任务未完成
                    69198, //用户经验已经领取
                }
            );
    }

    [Fact]
    public async Task CompleteAsync_Normal_Success()
    {
        // Arrange
        var req = new ReceiveOrCompleteTaskRequest("dress-view");

        // Act
        var re = await _api.VipBigPointCompleteAsync(req, _ck.ToString());

        // Assert
        re.Code.Should().Be(0);
    }
}
