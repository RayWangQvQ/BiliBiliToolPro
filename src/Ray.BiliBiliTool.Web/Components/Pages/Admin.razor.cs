using Microsoft.AspNetCore.Components;
using MudBlazor;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Components.Pages;

public partial class Admin : ComponentBase
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private IAuthService AuthService { get; set; } = null!;

    private string _username = "";
    private string _currentPassword = "";
    private string _newPassword = "";
    private string _confirmPassword = "";
    private string _errorMessage = "";
    private string _successMessage = "";

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

        if (_newPassword != _confirmPassword)
        {
            _errorMessage = "The new password and the confirm password do not match";
            return;
        }

        if (string.IsNullOrWhiteSpace(_newPassword))
        {
            _errorMessage = "Password cannot be empty";
            return;
        }

        try
        {
            await AuthService.ChangePasswordAsync(_username, _currentPassword, _newPassword);
            _successMessage = "Update Successful, you will be logged out in 2 seconds";
            await Task.Delay(2000);
            _currentPassword = "";
            _newPassword = "";
            _confirmPassword = "";
            NavigationManager.NavigateTo("/auth/logout", true);
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
        }
    }
}
