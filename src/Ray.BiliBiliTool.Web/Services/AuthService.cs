using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Ray.BiliBiliTool.Infrastructure.EF;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.Web.Services;

public interface IAuthService
{
    Task<ClaimsIdentity> LoginAsync(string username, string password);
}

public class AuthService(IDbContextFactory<BiliDbContext> dbFactory) : IAuthService
{
    public async Task<ClaimsIdentity> LoginAsync(string username, string password)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user != null && PasswordHelper.VerifyPassword(password, user.Salt, user.PasswordHash))
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, username) };
            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return claimsIdentity;
        }

        return new ClaimsIdentity();
    }
}
