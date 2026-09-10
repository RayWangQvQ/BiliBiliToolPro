using Microsoft.AspNetCore.Components;
using MudBlazor;
using Ray.BiliBiliTool.Web.Services;
using Ray.BiliBiliTool.Web.Services.Pages.Admin;

namespace Ray.BiliBiliTool.Web.Components.Pages;

public partial class Admin : ComponentBase
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private IAuthService AuthService { get; set; } = null!;

    [Inject]
    private IAdminPageWorkflow AdminPageWorkflow { get; set; } = null!;

    private string _username = "";
    private string _currentPassword = "";
    private string _newPassword = "";
    private string _confirmPassword = "";
    private string _errorMessage = "";
    private string _successMessage = "";
    private bool _showLogoutButton;

    private bool _passwordVisibility;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    private bool _currentPasswordVisibility;
    private InputType _currentPasswordInput = InputType.Password;
    private string _currentPasswordInputIcon = Icons.Material.Filled.VisibilityOff;

    private void TogglePasswordVisibility()
    {
        if (_passwordVisibility)
        {
            _passwordVisibility = false;
            _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
            _passwordInput = InputType.Password;
        }
        else
        {
            _passwordVisibility = true;
            _passwordInputIcon = Icons.Material.Filled.Visibility;
            _passwordInput = InputType.Text;
        }
    }

    private void ToggleCurrentPasswordVisibility()
    {
        if (_currentPasswordVisibility)
        {
            _currentPasswordVisibility = false;
            _currentPasswordInputIcon = Icons.Material.Filled.VisibilityOff;
            _currentPasswordInput = InputType.Password;
        }
        else
        {
            _currentPasswordVisibility = true;
            _currentPasswordInputIcon = Icons.Material.Filled.Visibility;
            _currentPasswordInput = InputType.Text;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _username = await AuthService.GetAdminUserNameAsync();
    }

    private async Task ChangePasswordAsync()
    {
        _errorMessage = "";
        _successMessage = "";
        _showLogoutButton = false;

        var request = new AdminPasswordChangeRequest(
            _username,
            _currentPassword,
            _newPassword,
            _confirmPassword
        );
        var result = await AdminPageWorkflow.ChangePasswordAsync(request);

        if (result.IsSuccess)
        {
            _successMessage = result.SuccessMessage ?? "Password updated successfully.";
            _showLogoutButton = true;
        }
        else
        {
            _errorMessage = result.ErrorMessage ?? "";
        }
    }

    private void Logout()
    {
        NavigationManager.NavigateTo("/auth/logout", true);
    }
}
