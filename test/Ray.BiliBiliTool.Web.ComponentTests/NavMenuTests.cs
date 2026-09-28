using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Web.Components.Layout;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

/// <summary>
/// 侧边栏底部常驻的「应用版本」。
/// </summary>
public class NavMenuTests : TestContext
{
    private const string TestVersion = "4.0.8-alpha.3";

    public NavMenuTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void NavMenu_Renders_ShowsAppVersion()
    {
        Services.AddSingleton<IAppInfoProvider>(new FakeAppInfoProvider(TestVersion));

        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().Contain($"版本号：{TestVersion}");
    }

    [Fact]
    public void NavMenu_LocalBuild_ShowsFallbackText()
    {
        Services.AddSingleton<IAppInfoProvider>(
            new FakeAppInfoProvider(AppVersion.LocalBuildDisplay)
        );

        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().Contain(AppVersion.LocalBuildDisplay);
        cut.Markup.Should().NotContain("0.0.0-dev");
    }
}
