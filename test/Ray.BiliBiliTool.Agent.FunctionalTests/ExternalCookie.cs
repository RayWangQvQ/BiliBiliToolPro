using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Xunit;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

internal static class ExternalCookie
{
    public static BiliCookie Require(IServiceProvider services)
    {
        var cookies = services.GetRequiredService<CookieStrFactory<BiliCookie>>();
        Assert.True(cookies.Count > 0, "External live tests require a configured Bilibili cookie.");
        var cookie = cookies.GetCookie(0);
        cookie.Check();
        return cookie;
    }
}
