using Bunit;
using FluentAssertions;
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

    private sealed class FakeLoginPageStateFactory(LoginPageState state) : ILoginPageStateFactory
    {
        public LoginPageState Create(Uri uri) => state;
    }
}
