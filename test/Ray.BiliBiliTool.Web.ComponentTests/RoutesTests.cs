using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components;
using Ray.BiliBiliTool.Web.Services;
using Ray.BiliBiliTool.Web.Services.Pages.Login;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class RoutesTests : ComponentTestBase
{
    [Fact]
    public void RenderComponent_UnauthorizedAdmin_RedirectsToLoginWithReturnUrl()
    {
        Services.AddMudServices();
        Services.AddSingleton<IAppInfoProvider>(new FakeAppInfoProvider("test"));
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddAuthorization().SetNotAuthorized();
        Services.AddSingleton<ILoginPageStateFactory>(new LoginPageStateFactory());
        var navigation = ((IServiceProvider)Services).GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/Admin?tab=account");

        var cut = Render<Routes>();

        cut.WaitForAssertion(() =>
            navigation
                .Uri.Should()
                .Be(navigation.BaseUri + "login?returnUrl=%2FAdmin%3Ftab%3Daccount")
        );
        cut.Find("input[name=returnUrl]").GetAttribute("value").Should().Be("/Admin?tab=account");
        cut.Markup.Should().NotContain("app-sidebar");
    }

    [Fact]
    public void RenderComponent_LoginRoute_UsesDedicatedLayoutWithoutBusinessNavigation()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddAuthorization().SetNotAuthorized();
        Services.AddSingleton<ILoginPageStateFactory>(new LoginPageStateFactory());
        ((IServiceProvider)Services).GetRequiredService<NavigationManager>().NavigateTo("/login");

        var cut = Render<Routes>();

        cut.Markup.Should().Contain("app-login-shell");
        cut.Markup.Should().NotContain("app-sidebar");
        cut.Markup.Should().NotContain("退出登录");
    }

    [Fact]
    public void RenderComponent_LoginWithSavedDarkMode_UsesSavedPreference()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<string>("localStorage.getItem", "bilitool-dark").SetResult("1");
        this.AddAuthorization().SetNotAuthorized();
        Services.AddSingleton<ILoginPageStateFactory>(new LoginPageStateFactory());
        ((IServiceProvider)Services).GetRequiredService<NavigationManager>().NavigateTo("/login");

        var cut = Render<Routes>();

        cut.WaitForAssertion(() =>
            cut.FindComponent<MudThemeProvider>()
                .Instance.GetState(x => x.IsDarkMode)
                .Should()
                .BeTrue()
        );
    }

    [Fact]
    public void RenderComponent_AboutRoute_FocusesHeadingForAssistiveTechnology()
    {
        Services.AddMudServices();
        Services.AddSingleton<IAppInfoProvider>(new FakeAppInfoProvider("test"));
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddAuthorization().SetAuthorized("admin");
        ((IServiceProvider)Services).GetRequiredService<NavigationManager>().NavigateTo("/about");

        var cut = Render<Routes>();

        cut.Find("h1").TextContent.Should().Contain("关于 BiliTool");
        JSInterop
            .Invocations.Select(invocation => invocation.Identifier)
            .Should()
            .Contain("Blazor._internal.domWrapper.focusBySelector");
    }
}
