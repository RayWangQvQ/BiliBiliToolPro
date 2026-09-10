using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.Web.Services;

public interface IAuthService
{
    Task<ClaimsIdentity> LoginAsync(string username, string password);
    Task ChangePasswordAsync(string username, string currentPassword, string newPassword);
    Task<string> GetAdminUserNameAsync();
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

    public async Task ChangePasswordAsync(
        string username,
        string currentPassword,
        string newPassword
    )
    {
        var user = await userRepository.GetAdminAsync();

        if (!PasswordHelper.VerifyPassword(currentPassword, user.Salt, user.PasswordHash))
        {
            throw new Exception("Current password is incorrect.");
        }

        var (hash, salt) = PasswordHelper.HashPassword(newPassword);

        user.Salt = salt;
        user.PasswordHash = hash;
        user.Username = username;

        await userRepository.UpdateAsync(user);
    }

    public async Task<string> GetAdminUserNameAsync()
    {
        var user = await userRepository.GetAdminAsync();
        return user.Username;
    }
}
