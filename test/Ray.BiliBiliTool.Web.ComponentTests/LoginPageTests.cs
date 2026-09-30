using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components.Pages;
using Ray.BiliBiliTool.Web.Services.Pages.Login;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

/// <summary>
/// Baseline component tests for the Login page.
/// Pin current observable behavior before Phase 13–15 boundary refactors begin.
/// Uses a fake <see cref="ILoginPageStateFactory"/> so the tests remain valid
/// regardless of URL parsing implementation details.
/// </summary>
public class LoginPageTests : TestContext
{
    public LoginPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddTestAuthorization().SetNotAuthorized();
    }

    [Fact]
    public void Login_WithNoError_RendersWithoutErrorAlert()
    {
        Services.AddSingleton<ILoginPageStateFactory>(
            new FakeLoginPageStateFactory(new LoginPageState(ReturnUrl: null, HasLoginError: false))
        );

        var cut = RenderComponent<Login>();

        cut.Markup.Should().NotContain("Incorrect username or password");
    }

    [Fact]
    public void Login_WithHasLoginErrorTrue_RendersErrorAlert()
    {
        Services.AddSingleton<ILoginPageStateFactory>(
            new FakeLoginPageStateFactory(new LoginPageState(ReturnUrl: null, HasLoginError: true))
        );

        var cut = RenderComponent<Login>();

        cut.Markup.Should().Contain("Incorrect username or password");
    }

    [Fact]
    public void Login_RendersPasswordFieldWithVisibilityToggle()
    {
        Services.AddSingleton<ILoginPageStateFactory>(
            new FakeLoginPageStateFactory(new LoginPageState(ReturnUrl: null, HasLoginError: false))
        );

        var cut = RenderComponent<Login>();

        // Password field has an adornment icon-button for visibility toggling
        cut.FindAll("button.mud-icon-button").Should().NotBeEmpty();
    }

    [Fact]
    public void Login_WhenAlreadyAuthenticated_NavigatesHome()
    {
        this.AddTestAuthorization().SetAuthorized("admin");
        Services.AddSingleton<ILoginPageStateFactory>(
            new FakeLoginPageStateFactory(new LoginPageState(ReturnUrl: null, HasLoginError: false))
        );
        var navigation = ((IServiceProvider)Services).GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/login");

        RenderComponent<Login>();

        navigation.Uri.Should().Be(navigation.BaseUri);
    }

    [Fact]
    public void Login_WithReturnUrl_PreservesItInPostForm()
    {
        Services.AddSingleton<ILoginPageStateFactory>(new LoginPageStateFactory());
        ((IServiceProvider)Services)
            .GetRequiredService<NavigationManager>()
            .NavigateTo("/login?returnUrl=%2FAdmin%3Ftab%3Daccount");

        var cut = RenderComponent<Login>();

        cut.Find("input[name=returnUrl]").GetAttribute("value").Should().Be("/Admin?tab=account");
    }

    private sealed class FakeLoginPageStateFactory(LoginPageState state) : ILoginPageStateFactory
    {
        public LoginPageState Create(Uri uri) => state;
    }
}
