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
        var request = new AdminPasswordChangeRequest("admin", "current", "", "");

        var result = await workflow.ChangePasswordAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Password cannot be empty");
    }

    [Fact]
    public async Task ChangePasswordAsync_WhitespaceNewPassword_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var request = new AdminPasswordChangeRequest("admin", "current", "   ", "   ");

        var result = await workflow.ChangePasswordAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Password cannot be empty");
    }

    [Fact]
    public async Task ChangePasswordAsync_MismatchedPasswords_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var request = new AdminPasswordChangeRequest("admin", "current", "newpass1", "newpass2");

        var result = await workflow.ChangePasswordAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("The new password and the confirm password do not match");
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_CallsAuthServiceAndReturnsSuccess()
    {
        var fake = new FakeAuthService();
        var workflow = CreateWorkflow(fake);
        var request = new AdminPasswordChangeRequest("admin", "current", "newpass", "newpass");

        var result = await workflow.ChangePasswordAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.SuccessMessage.Should().NotBeNullOrEmpty();
        fake.ChangePasswordCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_AuthServiceThrows_ReturnsErrorWithMessage()
    {
        var fake = new FakeAuthService(throwMessage: "Current password is incorrect.");
        var workflow = CreateWorkflow(fake);
        var request = new AdminPasswordChangeRequest("admin", "wrong", "newpass", "newpass");

        var result = await workflow.ChangePasswordAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Current password is incorrect.");
    }

    private sealed class FakeAuthService(string? throwMessage = null) : IAuthService
    {
        public bool ChangePasswordCalled { get; private set; }

        public Task<ClaimsIdentity> LoginAsync(string u, string p) =>
            Task.FromResult(new ClaimsIdentity());

        public Task ChangePasswordAsync(string u, string c, string n)
        {
            if (throwMessage is not null)
                throw new Exception(throwMessage);
            ChangePasswordCalled = true;
            return Task.CompletedTask;
        }

        public Task<string> GetAdminUserNameAsync() => Task.FromResult("admin");
    }
}
