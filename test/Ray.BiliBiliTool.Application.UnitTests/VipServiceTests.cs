using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Application.UnitTests;

[Trait("Category", "External")]
public class VipServiceTests
{
    public VipServiceTests()
    {
        Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
    }

    [Fact]
    public async Task VipBigPointCompleteV2_ConfiguredAccount_ReturnsSuccess()
    {
        using var scope = Assert
            .IsAssignableFrom<IServiceProvider>(Global.ServiceProviderRoot)
            .CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApiApi>();
        var res = await api.VipBigPointCompleteV2(
            new VipPointV2TaskRequest("dress-view"),
            GetConfiguredCookie(scope.ServiceProvider)
        );
        Assert.Equal(0, res.Code);
    }

    [Fact]
    public async Task VipBigPointReceiveV2_ConfiguredAccount_ReturnsSuccess()
    {
        using var scope = Assert
            .IsAssignableFrom<IServiceProvider>(Global.ServiceProviderRoot)
            .CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApiApi>();
        var res = await api.VipBigPointReceiveV2(
            new VipPointV2TaskRequest("ogvwatchnew"),
            GetConfiguredCookie(scope.ServiceProvider)
        );
        Assert.Equal(0, res.Code);
    }

    private static string GetConfiguredCookie(IServiceProvider services)
    {
        var cookies = services.GetRequiredService<CookieStrFactory<BiliCookie>>();
        Assert.True(cookies.Count > 0, "Requires a configured BiliBili account cookie");
        var cookie = cookies.GetCookie(0);
        cookie.Check();
        return cookie.ToString();
    }
}
