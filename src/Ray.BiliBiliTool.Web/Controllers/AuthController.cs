using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromForm] string username,
        [FromForm] string password,
        [FromForm] string? returnUrl
    )
    {
        var claimsIdentity = await authService.LoginAsync(username, password);

        if (claimsIdentity.IsAuthenticated)
        {
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/",
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";

            return Redirect(returnUrl);
        }

        return Redirect($"/login?error=true&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/login");
    }
}
