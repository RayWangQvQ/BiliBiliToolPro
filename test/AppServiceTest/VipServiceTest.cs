using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Dtos;
using Ray.BiliBiliTool.Infrastructure;

namespace AppServiceTest;

public class VipServiceTest
{
    public VipServiceTest()
    {
        Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
    }

    [Fact]
    public async Task VipExpressTest()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();
        var appService = scope.ServiceProvider.GetRequiredService<IVipBigPointAppService>();
        await appService.VipExpress();
    }

    [Fact]
    public async Task WatchVideo()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();
        var appService = scope.ServiceProvider.GetRequiredService<IVipBigPointAppService>();
        var res = await appService.WatchBangumi();
    }


    [Fact]
    public async Task CompleteV2Test()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IVipBigPointApi>();
        var res = await api.CompleteV2(new ReceiveOrCompleteTaskRequest("dress-view"));
        Assert.True(res.Code == 0);
    }

    [Fact]
    public async Task ReceiveV2Test()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IVipBigPointApi>();
        var res = await api.ReceiveV2(new ReceiveOrCompleteTaskRequest("ogvwatchnew"));
        Assert.True(res.Code == 0);
    }
}
