using System.Security.Claims;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components.Pages;
using Ray.BiliBiliTool.Web.Services;
using Ray.BiliBiliTool.Web.Services.Pages.Admin;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

/// <summary>
/// Component tests for the Admin page.
/// Uses FakeAdminPageWorkflow to control the outcome of each account mutation
/// and FakeAuthService for the account header (OnInitializedAsync).
/// </summary>
public class AdminPageTests : TestContext
{
    private const string TestUsername = "testadmin";

    private const string PasswordButtonLabel = "修改密码";
    private const string UsernameButtonLabel = "保存用户名";

    private readonly FakeAdminPageWorkflow _workflow = new();

    public AdminPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IAuthService>(new FakeAuthService());
        Services.AddSingleton<IAdminPageWorkflow>(_workflow);
    }

    [Fact]
    public void RenderComponent_AuthenticatedAdmin_DisplaysAccountFromAuthService()
    {
        var cut = RenderComponent<Admin>();

        cut.Markup.Should().Contain(TestUsername);
        // The role is persisted as "Administrator" and must not leak into the UI untranslated.
        cut.Markup.Should().Contain("管理员");
        cut.Markup.Should().NotContain("Administrator");
    }

    [Fact]
    public void RenderComponent_DefaultState_RendersAccountAndPasswordForms()
    {
        var cut = RenderComponent<Admin>();

        cut.FindAll("#admin-new-username").Count.Should().Be(1);
        cut.FindAll("#admin-rename-current-password").Count.Should().Be(1);
        cut.FindAll("#admin-current-password").Count.Should().Be(1);
        cut.FindAll("#admin-new-password").Count.Should().Be(1);
        cut.FindAll("#admin-confirm-password").Count.Should().Be(1);
        cut.FindAll("button.mud-button-filled").Count.Should().Be(2);
    }

    [Fact]
    public async Task Submit_PasswordWorkflowError_ShowsErrorSnackbar()
    {
        _workflow.Result = new AdminAccountChangeResult(false, "当前密码不正确", null);
        var cut = RenderComponent<Admin>();
        FillPasswordForm(cut, current: "wrong", newPassword: "newpass", confirmation: "newpass");

        await Submit(cut, PasswordButtonLabel);

        _workflow.PasswordCalled.Should().BeTrue();
        LastSnackbarMessage().Should().Be("当前密码不正确");
        GetRequiredService<NavigationManager>().Uri.Should().NotEndWith("/auth/logout");
    }

    [Fact]
    public async Task Submit_PasswordWorkflowSuccess_ShowsSuccessSnackbarAndLogsOut()
    {
        _workflow.Result = new AdminAccountChangeResult(true, null, "密码修改成功");
        var cut = RenderComponent<Admin>();
        FillPasswordForm(cut, current: "current", newPassword: "newpass", confirmation: "newpass");

        await Submit(cut, PasswordButtonLabel);

        LastSnackbarMessage().Should().Be("密码修改成功");
        // The session was authenticated with the old password, so it must be re-established.
        GetRequiredService<NavigationManager>().Uri.Should().EndWith("/auth/logout");
    }

    [Fact]
    public async Task Submit_MismatchedPasswordConfirmation_DoesNotCallWorkflow()
    {
        _workflow.Result = new AdminAccountChangeResult(true, null, "密码修改成功");
        var cut = RenderComponent<Admin>();
        FillPasswordForm(cut, current: "current", newPassword: "newpass", confirmation: "other");

        await Submit(cut, PasswordButtonLabel);

        _workflow.PasswordCalled.Should().BeFalse();
        LastSnackbarMessage().Should().BeNull();
        cut.Markup.Should().Contain("两次输入的新密码不一致");
    }

    [Fact]
    public async Task Submit_EmptyPasswordFields_DoesNotCallWorkflow()
    {
        _workflow.Result = new AdminAccountChangeResult(true, null, "密码修改成功");
        var cut = RenderComponent<Admin>();

        await Submit(cut, PasswordButtonLabel);

        _workflow.PasswordCalled.Should().BeFalse();
        cut.Markup.Should().Contain("请输入当前密码");
    }

    [Fact]
    public async Task Submit_UsernameWorkflowSuccess_ShowsMessageAndLogsOut()
    {
        _workflow.Result = new AdminAccountChangeResult(true, null, "用户名已更新");
        var cut = RenderComponent<Admin>();
        FillUsernameForm(cut, newUsername: "renamed", currentPassword: "current");

        await Submit(cut, UsernameButtonLabel);

        _workflow.UsernameCalled.Should().BeTrue();
        _workflow.LastUsernameRequest!.NewUsername.Should().Be("renamed");
        LastSnackbarMessage().Should().Be("用户名已更新");
        // The cookie still carries the old name, so the page must force a re-login.
        GetRequiredService<NavigationManager>().Uri.Should().EndWith("/auth/logout");
    }

    [Fact]
    public async Task Submit_UsernameWorkflowError_StaysOnPage()
    {
        _workflow.Result = new AdminAccountChangeResult(false, "当前密码不正确", null);
        var cut = RenderComponent<Admin>();
        FillUsernameForm(cut, newUsername: "renamed", currentPassword: "wrong");

        await Submit(cut, UsernameButtonLabel);

        _workflow.UsernameCalled.Should().BeTrue();
        LastSnackbarMessage().Should().Be("当前密码不正确");
        GetRequiredService<NavigationManager>().Uri.Should().NotEndWith("/auth/logout");
    }

    private static void FillPasswordForm(
        IRenderedComponent<Admin> cut,
        string current,
        string newPassword,
        string confirmation
    )
    {
        cut.Find("#admin-current-password").Change(current);
        cut.Find("#admin-new-password").Change(newPassword);
        cut.Find("#admin-confirm-password").Change(confirmation);
    }

    private static void FillUsernameForm(
        IRenderedComponent<Admin> cut,
        string newUsername,
        string currentPassword
    )
    {
        cut.Find("#admin-new-username").Change(newUsername);
        cut.Find("#admin-rename-current-password").Change(currentPassword);
    }

    private static async Task Submit(IRenderedComponent<Admin> cut, string label)
    {
        var button = cut.FindAll("button.mud-button-filled")
            .Single(b => b.TextContent.Contains(label));

        await button.ClickAsync(new());
    }

    /// <summary>
    /// bUnit's TestServiceProvider is both an IServiceProvider and an IServiceCollection,
    /// which makes the GetRequiredService extension methods ambiguous.
    /// </summary>
    private T GetRequiredService<T>()
        where T : notnull => ((IServiceProvider)Services).GetRequiredService<T>();

    private string? LastSnackbarMessage() =>
        GetRequiredService<ISnackbar>().ShownSnackbars.LastOrDefault()?.Message;

    private sealed class FakeAdminPageWorkflow : IAdminPageWorkflow
    {
        public AdminAccountChangeResult Result { get; set; } = new(false, null, null);

        public bool PasswordCalled { get; private set; }

        public bool UsernameCalled { get; private set; }

        public AdminUsernameChangeRequest? LastUsernameRequest { get; private set; }

        public Task<AdminAccountChangeResult> ChangePasswordAsync(
            AdminPasswordChangeRequest request
        )
        {
            PasswordCalled = true;
            return Task.FromResult(Result);
        }

        public Task<AdminAccountChangeResult> ChangeUsernameAsync(
            AdminUsernameChangeRequest request
        )
        {
            UsernameCalled = true;
            LastUsernameRequest = request;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeAuthService : IAuthService
    {
        public Task<ClaimsIdentity> LoginAsync(string u, string p) =>
            Task.FromResult(new ClaimsIdentity());

        public Task<AdminAccountInfo> GetAdminAccountAsync() =>
            Task.FromResult(new AdminAccountInfo(TestUsername, ["Administrator"]));

        public Task ChangePasswordAsync(string currentPassword, string newPassword) =>
            Task.CompletedTask;

        public Task ChangeUsernameAsync(string newUsername, string currentPassword) =>
            Task.CompletedTask;
    }
}
