using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Ray.BiliBiliTool.Web.Services.Pages.Login;

namespace Ray.BiliBiliTool.Web.Components.Pages;

public partial class Login : ComponentBase
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private ILoginPageStateFactory LoginPageStateFactory { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    private string _username = "";
    private string _password = "";

    private bool _passwordVisibility;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

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

    private string? returnUrl;
    private bool _loginError = false;

    protected override async Task OnInitializedAsync()
    {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authenticationState.User.Identity?.IsAuthenticated == true)
        {
            NavigationManager.NavigateTo("/", replace: true);
            return;
        }

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var state = LoginPageStateFactory.Create(uri);
        returnUrl = state.ReturnUrl;
        _loginError = state.HasLoginError;
    }
}
