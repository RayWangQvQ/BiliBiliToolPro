using Microsoft.AspNetCore.WebUtilities;

namespace Ray.BiliBiliTool.Web.Services.Pages.Login;

/// <summary>
/// Concrete implementation of <see cref="ILoginPageStateFactory"/>.
/// Centralises query-string parsing so the Login component code-behind
/// has a single audited place for returnUrl and error-flag handling.
/// </summary>
public sealed class LoginPageStateFactory : ILoginPageStateFactory
{
    public LoginPageState Create(Uri uri)
    {
        var query = QueryHelpers.ParseQuery(uri.Query);

        string? returnUrl = null;
        if (query.TryGetValue("returnUrl", out var returnUrlParam))
            returnUrl = returnUrlParam.FirstOrDefault();

        bool hasLoginError = false;
        if (
            query.TryGetValue("error", out var errorParam)
            && bool.TryParse(errorParam.FirstOrDefault(), out var parsed)
            && parsed
        )
        {
            hasLoginError = true;
        }

        return new LoginPageState(returnUrl, hasLoginError);
    }
}
