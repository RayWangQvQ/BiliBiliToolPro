using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
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
    public async Task CompleteV2Test()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApiApi>();
        var res = await api.VipBigPointCompleteV2(
            new ReceiveOrCompleteTaskRequest("dress-view"),
            null
        );
        Assert.True(res.Code == 0);
    }

    [Fact]
    public async Task ReceiveV2Test()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApiApi>();
        var res = await api.VipBigPointReceiveV2(
            new ReceiveOrCompleteTaskRequest("ogvwatchnew"),
            null
        );
        Assert.True(res.Code == 0);
    }
}
