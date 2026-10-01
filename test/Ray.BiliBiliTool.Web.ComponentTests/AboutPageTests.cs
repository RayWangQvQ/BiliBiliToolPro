using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components.Pages;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

/// <summary>
/// /about：版本号 + 开源链接，且不需要登录即可访问（页面本身不加 [Authorize]）。
/// </summary>
public class AboutPageTests : TestContext
{
    private const string TestVersion = "4.0.8-alpha.3";

    public AboutPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void RenderComponent_ConfiguredVersion_ShowsAppVersion()
    {
        Services.AddSingleton<IAppInfoProvider>(new FakeAppInfoProvider(TestVersion));

        var cut = RenderComponent<About>();

        cut.Markup.Should().Contain($"版本号：{TestVersion}");
    }

    [Fact]
    public void RenderComponent_DefaultState_LinksToSourceRepo()
    {
        Services.AddSingleton<IAppInfoProvider>(new FakeAppInfoProvider(TestVersion));

        var cut = RenderComponent<About>();

        cut.Markup.Should().Contain("github.com/RayWangQvQ/BiliBiliToolPro");
    }
}
