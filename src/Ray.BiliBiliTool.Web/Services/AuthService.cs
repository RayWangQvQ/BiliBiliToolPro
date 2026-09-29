using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.Web.Services;

public interface IAuthService
{
    Task<ClaimsIdentity> LoginAsync(string username, string password);

    Task<AdminAccountInfo> GetAdminAccountAsync();

    /// <summary>Rewrites the password hash only - the login name is left untouched.</summary>
    Task ChangePasswordAsync(string currentPassword, string newPassword);

    /// <summary>Rewrites the login name only - the password hash is left untouched.</summary>
    Task ChangeUsernameAsync(string newUsername, string currentPassword);
}

public class AuthService(IUserRepository userRepository) : IAuthService
{
    public async Task<ClaimsIdentity> LoginAsync(string username, string password)
    {
        var user = await userRepository.FindByUsernameAsync(username);

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

    public async Task<AdminAccountInfo> GetAdminAccountAsync()
    {
        var user = await userRepository.GetAdminAsync();
        return new AdminAccountInfo(user.Username, user.Roles);
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var user = await userRepository.GetAdminAsync();
        EnsureCurrentPasswordMatches(user, currentPassword);

        var (hash, salt) = PasswordHelper.HashPassword(newPassword);

        user.Salt = salt;
        user.PasswordHash = hash;

        await userRepository.UpdateAsync(user);
    }

    public async Task ChangeUsernameAsync(string newUsername, string currentPassword)
    {
        var user = await userRepository.GetAdminAsync();
        EnsureCurrentPasswordMatches(user, currentPassword);

        user.Username = newUsername;

        await userRepository.UpdateAsync(user);
    }

    /// <summary>
    /// Both account mutations are authorised by the current password, so a stolen
    /// session cookie alone cannot rewrite the credentials.
    /// </summary>
    private static void EnsureCurrentPasswordMatches(User user, string currentPassword)
    {
        if (!PasswordHelper.VerifyPassword(currentPassword, user.Salt, user.PasswordHash))
        {
            throw new InvalidOperationException("当前密码不正确");
        }
    }
}
