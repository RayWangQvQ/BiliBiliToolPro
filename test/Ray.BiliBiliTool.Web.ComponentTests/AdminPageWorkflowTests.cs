using System.Security.Claims;
using FluentAssertions;
using Ray.BiliBiliTool.Web.Services;
using Ray.BiliBiliTool.Web.Services.Pages.Admin;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class AdminPageWorkflowTests
{
    private static AdminPageWorkflow CreateWorkflow(FakeAuthService? fake = null) =>
        new AdminPageWorkflow(fake ?? new FakeAuthService());

    [Fact]
    public async Task ChangePasswordAsync_EmptyNewPassword_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var request = new AdminPasswordChangeRequest("current", "", "");

        var result = await workflow.ChangePasswordAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("新密码不能为空");
    }

    [Fact]
    public async Task ChangePasswordAsync_WhitespaceNewPassword_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var request = new AdminPasswordChangeRequest("current", "   ", "   ");

        var result = await workflow.ChangePasswordAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("新密码不能为空");
    }

    [Fact]
    public async Task ChangePasswordAsync_MismatchedPasswords_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var request = new AdminPasswordChangeRequest("current", "newpass1", "newpass2");

        var result = await workflow.ChangePasswordAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("两次输入的新密码不一致");
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_CallsAuthServiceAndReturnsSuccess()
    {
        var fake = new FakeAuthService();
        var workflow = CreateWorkflow(fake);
        var request = new AdminPasswordChangeRequest("current", "newpass", "newpass");

        var result = await workflow.ChangePasswordAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.SuccessMessage.Should().Be("密码修改成功");
        fake.ChangePasswordCalled.Should().BeTrue();
        fake.LastCurrentPassword.Should().Be("current");
        fake.LastNewPassword.Should().Be("newpass");
    }

    [Fact]
    public async Task ChangePasswordAsync_AuthServiceThrows_ReturnsErrorWithMessage()
    {
        var fake = new FakeAuthService(throwMessage: "当前密码不正确");
        var workflow = CreateWorkflow(fake);
        var request = new AdminPasswordChangeRequest("wrong", "newpass", "newpass");

        var result = await workflow.ChangePasswordAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("当前密码不正确");
    }

    [Fact]
    public async Task ChangeUsernameAsync_EmptyNewUsername_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var request = new AdminUsernameChangeRequest("", "current");

        var result = await workflow.ChangeUsernameAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("新用户名不能为空");
    }

    [Fact]
    public async Task ChangeUsernameAsync_WhitespaceNewUsername_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var request = new AdminUsernameChangeRequest("   ", "current");

        var result = await workflow.ChangeUsernameAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("新用户名不能为空");
    }

    [Fact]
    public async Task ChangeUsernameAsync_ValidRequest_TrimsNameAndReturnsSuccess()
    {
        var fake = new FakeAuthService();
        var workflow = CreateWorkflow(fake);
        var request = new AdminUsernameChangeRequest("  renamed  ", "current");

        var result = await workflow.ChangeUsernameAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.SuccessMessage.Should().Be("用户名已更新");
        fake.ChangeUsernameCalled.Should().BeTrue();
        fake.LastNewUsername.Should().Be("renamed");
    }

    [Fact]
    public async Task ChangeUsernameAsync_AuthServiceThrows_ReturnsErrorWithMessage()
    {
        var fake = new FakeAuthService(throwMessage: "当前密码不正确");
        var workflow = CreateWorkflow(fake);
        var request = new AdminUsernameChangeRequest("renamed", "wrong");

        var result = await workflow.ChangeUsernameAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("当前密码不正确");
    }

    private sealed class FakeAuthService(string? throwMessage = null) : IAuthService
    {
        public bool ChangePasswordCalled { get; private set; }

        public bool ChangeUsernameCalled { get; private set; }

        public string? LastCurrentPassword { get; private set; }

        public string? LastNewPassword { get; private set; }

        public string? LastNewUsername { get; private set; }

        public Task<ClaimsIdentity> LoginAsync(string u, string p) =>
            Task.FromResult(new ClaimsIdentity());

        public Task<AdminAccountInfo> GetAdminAccountAsync() =>
            Task.FromResult(new AdminAccountInfo("admin", ["Administrator"]));

        public Task ChangePasswordAsync(string currentPassword, string newPassword)
        {
            ThrowIfConfigured();
            ChangePasswordCalled = true;
            LastCurrentPassword = currentPassword;
            LastNewPassword = newPassword;
            return Task.CompletedTask;
        }

        public Task ChangeUsernameAsync(string newUsername, string currentPassword)
        {
            ThrowIfConfigured();
            ChangeUsernameCalled = true;
            LastCurrentPassword = currentPassword;
            LastNewUsername = newUsername;
            return Task.CompletedTask;
        }

        private void ThrowIfConfigured()
        {
            if (throwMessage is not null)
            {
                throw new InvalidOperationException(throwMessage);
            }
        }
    }
}
