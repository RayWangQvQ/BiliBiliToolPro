using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils
{
    public class LiveHeartBeatCrypto
    {
        public static string Sypder(string text, ICollection<int> rules, string key)
        {
            string result = text;
            foreach(var rule in rules)
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
                        break;
                }
            }
            return result;
        }

        private static string Hash(string text, string key, string algorithmName)
        {
            var hamc = HMAC.Create(algorithmName);
            hamc.Key = Encoding.UTF8.GetBytes(key);
            byte[] inArray = hamc.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(inArray).Replace("-", "").ToLower();
        }
    }
}
