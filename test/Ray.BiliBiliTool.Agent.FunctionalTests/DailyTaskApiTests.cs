using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

public class DailyTaskApiTests
{
    private readonly IDailyTaskApi _api;

    private readonly BiliCookie _ck;

    public DailyTaskApiTests()
    {
        var envs = new List<string>
        {
            "--ENVIRONMENT=Development",
            //"HTTP_PROXY=localhost:8888",
            //"HTTPS_PROXY=localhost:8888"
        };
        IHost host = Program.CreateHost(envs.ToArray());
        _ck = host.Services.GetRequiredService<BiliCookie>();
        _api = host.Services.GetRequiredService<IDailyTaskApi>();
    }

    [Fact]
    public async Task GetDailyTaskRewardInfo_Normal_Success()
    {
        // Act
        BiliApiResponse<DailyTaskInfo> re = await _api.GetDailyTaskRewardInfoAsync();

        // Arrange

        // Assert
        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDonateCoinExp_Normal_Success()
    {
        // Act
        BiliApiResponse<int> re = await _api.GetDonateCoinExpAsync();

        // Arrange

        // Assert
        re.Code.Should().Be(0);
        re.Data.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ReceiveVipPrivilege_Normal_Success()
    {
        // Act
        BiliApiResponse re = await _api.ReceiveVipPrivilegeAsync((int)VipPrivilegeType.BCoinCoupon, _ck.BiliJct);

        // Arrange

        // Assert
        re.Code.Should().BeOneOf(
            0,
            73319, //todo: sort out meannings
            69801 //你已领取过该权益
        );
    }
}
