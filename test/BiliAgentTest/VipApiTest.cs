using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Xunit.Abstractions;
using System;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ViewMall;

namespace BiliAgentTest;

public class VipApiTest
{
    private readonly ITestOutputHelper _output;
    public VipApiTest(ITestOutputHelper output)
    {
        _output = output;
        Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
    }

    [Fact]
    public async Task SignTaskTest()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();

        var ck = scope.ServiceProvider.GetRequiredService<BiliCookie>();
        var api = scope.ServiceProvider.GetRequiredService<IVipBigPointApi>();

        var re = await api.Sign(new SignRequest()
        {
            // Csrf = ck.BiliJct
        });
        _output.WriteLine(re.ToJsonStr());
        Assert.Equal(0, re.Code);
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
        var re = await api.ViewVipMall(new ViewVipMallRequest()
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
