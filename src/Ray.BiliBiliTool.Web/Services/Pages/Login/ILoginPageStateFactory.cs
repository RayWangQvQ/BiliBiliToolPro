namespace Ray.BiliBiliTool.Web.Services.Pages.Login;

/// <summary>
/// Web-layer factory that encapsulates query-string parsing for the Login page.
/// Keeps component code-behind free of URL manipulation logic and makes bUnit
/// stubbing trivial — a test double returns a pre-built <see cref="LoginPageState"/>.
/// </summary>
public interface ILoginPageStateFactory
{
    /// <summary>
    /// Creates the initial Login page state from the request URI.
    /// </summary>
    LoginPageState Create(Uri uri);
}
