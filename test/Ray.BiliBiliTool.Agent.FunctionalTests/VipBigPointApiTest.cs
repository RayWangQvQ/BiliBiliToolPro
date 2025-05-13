using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;
using Xunit.Abstractions;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

public class VipBigPointApiTest
{
    private readonly IVipBigPointApi _api;

    private readonly ITestOutputHelper _output;
    private readonly BiliCookie _ck;

    public VipBigPointApiTest(ITestOutputHelper output)
    {
        _output = output;

        var envs = new List<string>
        {
            "--ENVIRONMENT=Development",
            //"HTTP_PROXY=localhost:8888",
            //"HTTPS_PROXY=localhost:8888"
        };
        IHost host = Program.CreateHost(envs.ToArray());
        _ck = host.Services.GetRequiredService<BiliCookie>();
        _api = host.Services.GetRequiredService<IVipBigPointApi>();
    }

    [Fact]
    public async Task GetTaskListAsync_Normal_Success()
    {
        // Arrange
        // Act
        BiliApiResponse<VipTaskInfo> re = await _api.GetTaskListAsync(null);

        // Assert
        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
        re.Data.Task_info.Modules.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task SignAsync_Normal_Success()
    {
        // Arrange
        var req = new SignRequest() { csrf = _ck.BiliJct };

        // Act
        BiliApiResponse re = await _api.SignAsync(req, null);
        _output.WriteLine(re.ToJsonStr());

        // Assert
        re.Code.Should().Be(0);
        re.Message.Should().BeEquivalentTo("success");
    }

    [Fact]
    public async Task GetVouchersInfoAsync_Normal_Success()
    {
        // Arrange
        // Act
        var re = await _api.GetVouchersInfoAsync(null);

        // Assert
        re.Code.Should().Be(0);
        re.Data.List.Should().Contain(x => x.Type == 9);
    }

    [Fact]
    public async Task GetVipExperienceAsync_Normal_Success()
    {
        // Arrange
        var req = new VipExperienceRequest() { csrf = _ck.BiliJct };

        // Act
        BiliApiResponse re = await _api.ObtainVipExperienceAsync(req, null);

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
        var re = await _api.CompleteAsync(req, null);

        // Assert
        re.Code.Should().Be(0);
    }
}
