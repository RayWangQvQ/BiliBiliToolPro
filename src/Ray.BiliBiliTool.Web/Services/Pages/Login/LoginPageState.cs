namespace Ray.BiliBiliTool.Web.Services.Pages.Login;

/// <summary>
/// Immutable value object representing the initial display state of the Login page,
/// derived from query-string parameters by <see cref="ILoginPageStateFactory"/>.
/// </summary>
public sealed record LoginPageState(string? ReturnUrl, bool HasLoginError);
