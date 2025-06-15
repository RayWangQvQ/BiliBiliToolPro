using System.Security.Cryptography;
using System.Text;

namespace Ray.BiliBiliTool.Infrastructure.Helpers;

public class PasswordHelper
{
    public static (string hash, string salt) HashPassword(string password)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
        string salt = Convert.ToBase64String(saltBytes);
        string hash = ComputeHash(password, salt);
        return (hash, salt);
    }

    public static bool VerifyPassword(string password, string salt, string hash)
    {
        string computedHash = ComputeHash(password, salt);
        return computedHash == hash;
    }

    private static string ComputeHash(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
        byte[] hashBytes = sha256.ComputeHash(passwordBytes);
        return Convert.ToBase64String(hashBytes);
    }
}
