using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Ray.BiliBiliTool.Infrastructure.EF;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.Web.Services;

public interface IAuthService
{
    Task<ClaimsIdentity> LoginAsync(string username, string password);
    Task ChangePasswordAsync(string username, string currentPassword, string newPassword);
    Task<string> GetAdminUserNameAsync();
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

    public async Task ChangePasswordAsync(
        string username,
        string currentPassword,
        string newPassword
    )
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var user = await context.Users.FirstAsync(u => u.Id == 1);

        if (!PasswordHelper.VerifyPassword(currentPassword, user.Salt, user.PasswordHash))
        {
            throw new Exception("Current password is incorrect.");
        }

        var (hash, salt) = PasswordHelper.HashPassword(newPassword);

        user.Salt = salt;
        user.PasswordHash = hash;
        user.Username = username;

        await context.SaveChangesAsync();
    }

    public async Task<string> GetAdminUserNameAsync()
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var user = await context.Users.FirstAsync(u => u.Id == 1);
        return user.Username;
    }
}
