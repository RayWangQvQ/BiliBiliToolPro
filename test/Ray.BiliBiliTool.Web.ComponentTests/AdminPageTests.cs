using System.Security.Claims;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components.Pages;
using Ray.BiliBiliTool.Web.Services;
using Ray.BiliBiliTool.Web.Services.Pages.Admin;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

/// <summary>
/// Component tests for the Admin page.
/// Uses FakeAdminPageWorkflow to control workflow outcomes and
/// FakeAuthService for username loading (OnInitializedAsync).
/// </summary>
public class AdminPageTests : TestContext
{
    private const string TestUsername = "testadmin";

    public AdminPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IAuthService>(new FakeAuthService(TestUsername));
    }

    [Fact]
    public void Admin_OnInitialized_DisplaysUsernameFromAuthService()
    {
        Services.AddSingleton<IAdminPageWorkflow>(
            new FakeAdminPageWorkflow(new AdminPasswordChangeResult(false, null, null))
        );
        var cut = RenderComponent<Admin>();

        cut.Markup.Should().Contain(TestUsername);
    }

    [Fact]
    public async Task Admin_SubmitWithWorkflowReturningError_ShowsErrorMessage()
    {
        var errorResult = new AdminPasswordChangeResult(false, "Password cannot be empty", null);
        Services.AddSingleton<IAdminPageWorkflow>(new FakeAdminPageWorkflow(errorResult));
        var cut = RenderComponent<Admin>();

        await cut.Find("button.mud-button-filled").ClickAsync(new());

        cut.Markup.Should().Contain("Password cannot be empty");
    }

    [Fact]
    public async Task Admin_SubmitWithWorkflowReturningSuccess_ShowsLogoutButton()
    {
        var successResult = new AdminPasswordChangeResult(
            true,
            null,
            "Password updated successfully."
        );
        Services.AddSingleton<IAdminPageWorkflow>(new FakeAdminPageWorkflow(successResult));
        var cut = RenderComponent<Admin>();

        await cut.Find("button.mud-button-filled").ClickAsync(new());

        cut.Markup.Should().Contain("Logout");
        cut.Markup.Should().Contain("Password updated successfully.");
    }

    [Fact]
    public void Admin_RendersExpectedNumberOfInputFields()
    {
        Services.AddSingleton<IAdminPageWorkflow>(
            new FakeAdminPageWorkflow(new AdminPasswordChangeResult(false, null, null))
        );
        var cut = RenderComponent<Admin>();

        cut.FindAll("input").Count.Should().BeGreaterThanOrEqualTo(4);
    }

    private sealed class FakeAdminPageWorkflow(AdminPasswordChangeResult result)
        : IAdminPageWorkflow
    {
        public Task<AdminPasswordChangeResult> ChangePasswordAsync(
            AdminPasswordChangeRequest request
        ) => Task.FromResult(result);
    }

    private sealed class FakeAuthService(string username) : IAuthService
    {
        public Task<ClaimsIdentity> LoginAsync(string u, string p) =>
            Task.FromResult(new ClaimsIdentity());

        public Task ChangePasswordAsync(string u, string c, string n) => Task.CompletedTask;

        public Task<string> GetAdminUserNameAsync() => Task.FromResult(username);
    }
}
