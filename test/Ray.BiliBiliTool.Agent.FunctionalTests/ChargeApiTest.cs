using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

public class ChargeApiTest
{
    private readonly IChargeApi _target;

    private readonly BiliCookie _ck;

    public ChargeApiTest()
    {
        var envs = new List<string>
        {
            "--ENVIRONMENT=Development",
            //"HTTP_PROXY=localhost:8888",
            //"HTTPS_PROXY=localhost:8888"
        };
        IHost host = Program.CreateHost(envs.ToArray());
        _ck = host.Services.GetRequiredService<BiliCookie>();
        _target = host.Services.GetRequiredService<IChargeApi>();
    }

    #region ChargeV2Async

    [Fact]
    public async void ChargeV2Async_SendRequest_NotEnough()
    {
        // Arrange
        var upId = 220893216;
        var req = new ChargeRequest(2, upId, _ck.BiliJct);

        // Act
        BiliApiResponse<ChargeV2Response> re = await _target.ChargeV2Async(req, null);

        // Assert
        re.Code.Should().Be(0);
        re.Data.Status.Should()
            .BeOneOf(
                -4, //bp.to.battery http failed, invalid args, errNo=800409904: B ������
                4
            );
    }

    #endregion

    #region ChargeCommentAsync

    #endregion
}
