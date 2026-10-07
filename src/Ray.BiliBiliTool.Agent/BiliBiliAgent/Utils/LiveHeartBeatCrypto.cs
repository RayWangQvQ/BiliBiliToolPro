using System.Security.Cryptography;
using System.Text;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;

public class LiveHeartBeatCrypto
{
    public static string Sypder(string text, ICollection<int> rules, string key)
    {
        string result = text;
        foreach (var rule in rules)
        {
            switch (rule)
            {
                case 0:
                    result = Hash(result, key, "HMACMD5");
                    break;
                case 1:
                    result = Hash(result, key, "HMACSHA1");
                    break;
                case 2:
                    result = Hash(result, key, "HMACSHA256");
                    break;
                case 3:
                    result = Hash(result, key, "HMACSHA224");
                    break;
                case 4:
                    result = Hash(result, key, "HMACSHA512");
                    break;
                case 5:
                    result = Hash(result, key, "HMACSHA384");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(rules),
                        rule,
                        "Unsupported heartbeat signature rule"
                    );
            }
        }
        return result;
    }

    private static string Hash(string text, string key, string algorithmName)
    {
        if (algorithmName == "HMACSHA224")
            return Convert.ToHexStringLower(
                Sha224.Hmac(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(text))
            );
        HMAC hamc = algorithmName.ToUpperInvariant() switch
        {
            "HMACSHA256" => new HMACSHA256(Encoding.UTF8.GetBytes(key)),
            "HMACSHA1" => new HMACSHA1(Encoding.UTF8.GetBytes(key)),
            "HMACMD5" => new HMACMD5(Encoding.UTF8.GetBytes(key)),
            "HMACSHA384" => new HMACSHA384(Encoding.UTF8.GetBytes(key)),
            "HMACSHA512" => new HMACSHA512(Encoding.UTF8.GetBytes(key)),
            _ => throw new ArgumentException($"Unsupported algorithm: {algorithmName}"),
        };

        using HMAC hmac = hamc;
        byte[] hashBytes = hamc.ComputeHash(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
