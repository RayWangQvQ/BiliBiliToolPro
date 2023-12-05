using Microsoft.Extensions.DependencyInjection;
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
        Assert.True(res);
         
    }
}
