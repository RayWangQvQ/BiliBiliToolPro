using Microsoft.AspNetCore.Components;
using MudBlazor;
using Ray.BiliBiliTool.Web.Services;
using Ray.BiliBiliTool.Web.Services.Pages.Admin;

namespace Ray.BiliBiliTool.Web.Components.Pages;

public partial class Admin : ComponentBase
{
    /// <summary>How long the success message stays up before the forced re-login.</summary>
    private const int LogoutDelayMs = 1200;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private IAuthService AuthService { get; set; } = null!;

    [Inject]
    private IAdminPageWorkflow AdminPageWorkflow { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private AdminAccountInfo _account = new("", []);

    private MudForm? _usernameForm;
    private MudForm? _passwordForm;

    private string _newUsername = "";
    private readonly PasswordField _usernamePassword = new();
    private readonly PasswordField _currentPassword = new();
    private readonly PasswordField _newPassword = new();
    private readonly PasswordField _confirmPassword = new();

    private bool _usernameSubmitting;
    private bool _passwordSubmitting;

    protected override async Task OnInitializedAsync()
    {
        _account = await AuthService.GetAdminAccountAsync();
    }

    private static void Toggle(PasswordField field) => field.Toggle();

    /// <summary>Role names are persisted in English; the panel speaks Chinese.</summary>
    private static string DisplayRole(string role) =>
        role switch
        {
            "Administrator" => "管理员",
            _ => role,
        };

    /// <summary>Cross-field rule: the confirmation has to match the new password.</summary>
    private string? ValidateConfirmPassword(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "请再次输入新密码";
        }

        return value == _newPassword.Value ? null : "两次输入的新密码不一致";
    }

    private async Task ChangeUsernameAsync()
    {
        if (_usernameSubmitting || _usernameForm is null)
        {
            return;
        }

        await _usernameForm.ValidateAsync();
        if (!_usernameForm.IsValid)
        {
            return;
        }

        _usernameSubmitting = true;
        try
        {
            var result = await AdminPageWorkflow.ChangeUsernameAsync(
                new AdminUsernameChangeRequest(_newUsername, _usernamePassword.Value)
            );

            if (!result.IsSuccess)
            {
                Snackbar.Add(result.ErrorMessage ?? "修改用户名失败", Severity.Error);
                return;
            }

            // The auth cookie still carries the old name, so re-login instead of
            // leaving a stale ClaimTypes.Name behind. Show the message first.
            Snackbar.Add(result.SuccessMessage ?? "用户名已更新", Severity.Success);
            _account = _account with { Username = _newUsername.Trim() };
            _newUsername = "";
            await SignOutAfterDelayAsync();
        }
        finally
        {
            _usernameSubmitting = false;
        }
    }

    private async Task ChangePasswordAsync()
    {
        if (_passwordSubmitting || _passwordForm is null)
        {
            return;
        }

        await _passwordForm.ValidateAsync();
        if (!_passwordForm.IsValid)
        {
            return;
        }

        _passwordSubmitting = true;
        try
        {
            var result = await AdminPageWorkflow.ChangePasswordAsync(
                new AdminPasswordChangeRequest(
                    _currentPassword.Value,
                    _newPassword.Value,
                    _confirmPassword.Value
                )
            );

            if (!result.IsSuccess)
            {
                Snackbar.Add(result.ErrorMessage ?? "修改密码失败", Severity.Error);
                return;
            }

            Snackbar.Add(result.SuccessMessage ?? "密码修改成功", Severity.Success);
            _currentPassword.Value = "";
            _newPassword.Value = "";
            _confirmPassword.Value = "";

            // Same as the rename path: the session was authenticated with the old
            // credentials, so it has to be re-established with the new ones.
            await SignOutAfterDelayAsync();
        }
        finally
        {
            _passwordSubmitting = false;
        }
    }

    /// <summary>
    /// Both credential changes invalidate the current session, so the user is sent
    /// back to the login page once the success message has been readable for a moment.
    /// </summary>
    private async Task SignOutAfterDelayAsync()
    {
        await Task.Delay(LogoutDelayMs);
        NavigationManager.NavigateTo("/auth/logout", forceLoad: true);
    }

    /// <summary>
    /// One password input: value plus its own show/hide toggle, so the page does
    /// not carry six fields of visibility plumbing.
    /// </summary>
    private sealed class PasswordField
    {
        public string Value { get; set; } = "";

        private bool Visible { get; set; }

        public InputType InputType => Visible ? InputType.Text : InputType.Password;

        public string Icon =>
            Visible ? Icons.Material.Filled.Visibility : Icons.Material.Filled.VisibilityOff;

        public void Toggle() => Visible = !Visible;
    }
}
