using System.Security.Cryptography;

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
        byte[] saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            saltBytes,
            100_000,
            HashAlgorithmName.SHA256
        );
        byte[] hashBytes = pbkdf2.GetBytes(32);

        return Convert.ToBase64String(hashBytes);
    }
}
