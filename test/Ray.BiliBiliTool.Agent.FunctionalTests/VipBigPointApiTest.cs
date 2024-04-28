using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Xunit.Abstractions;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ViewMall;
using Microsoft.Extensions.Hosting;
using FluentAssertions;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

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
        BiliApiResponse<VipTaskInfo> re = await _api.GetTaskListAsync();

        // Assert
        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
        re.Data.Task_info.Modules.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task SignAsync_Normal_Success()
    {
        // Arrange
        var req = new SignRequest()
        {
            csrf = _ck.BiliJct
        };

        // Act
        BiliApiResponse re = await _api.SignAsync(req);
        _output.WriteLine(re.ToJsonStr());

        // Assert
        re.Code.Should().Be(0);
        re.Message.Should().BeEquivalentTo("success");
    }

    [Fact]
    public async Task VipInfoTest()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();

        var ck = scope.ServiceProvider.GetRequiredService<BiliCookie>();
        var api = scope.ServiceProvider.GetRequiredService<IVipBigPointApi>();

        var re = await api.GetVouchersInfo();
        if (re.Code == 0)
        {
            var info = re.Data.List.Find(x => x.Type == 9);
            if (info != null)
            {
                _output.WriteLine(info.State.ToString());
            }
            else
            {
                _output.WriteLine("error");
            }
        }
    }


    [Fact]
    public async Task GetVipExperienceTest()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();

        var ck = scope.ServiceProvider.GetRequiredService<BiliCookie>();
        var api = scope.ServiceProvider.GetRequiredService<IVipBigPointApi>();
        var re = await api.GetVipExperience(new VipExperienceRequest()
        {
            csrf = ck.BiliJct
        });

        _output.WriteLine(re.Message);
    }

    [Fact]
    public async Task ViewVipMallTest()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();

        var ck = scope.ServiceProvider.GetRequiredService<BiliCookie>();
        var api = scope.ServiceProvider.GetRequiredService<IVipMallApi>();
        // var test = await api.ViewvipMall("{\r\n\"csrf\":\"33e5d4564b6b69cb4ed829bc404158cb\",\r\n\"eventId\":\"hevent_oy4b7h3epeb\"\r\n}");
        var re = await api.ViewVipMallAsync(new ViewVipMallRequest()
        {
            Csrf = ck.BiliJct,
            EventId = "hevent_oy4b7h3epeb"
        });
        _output.WriteLine(re.Message);

    }

    [Fact]
    public async Task DressViewTest()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();

        var ck = scope.ServiceProvider.GetRequiredService<BiliCookie>();
        var api = scope.ServiceProvider.GetRequiredService<IVipBigPointApi>();
        var re = await api.Complete(new ReceiveOrCompleteTaskRequest(
            "dress-view"));
        _output.WriteLine(re.Message);
    }
}
