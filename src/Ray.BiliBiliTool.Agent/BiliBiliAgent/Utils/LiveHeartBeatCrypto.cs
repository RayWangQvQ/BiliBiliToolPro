using System;
using System.Collections.Generic;
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
                    break;
            }
        }
        return result;
    }

    private static string Hash(string text, string key, string algorithmName)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var textBytes = Encoding.UTF8.GetBytes(text);
        byte[] hash = algorithmName.ToUpperInvariant() switch
        {
            "HMACSHA256" => new HMACSHA256(keyBytes).ComputeHash(textBytes),
            "HMACSHA1" => new HMACSHA1(keyBytes).ComputeHash(textBytes),
            "HMACMD5" => new HMACMD5(keyBytes).ComputeHash(textBytes),
            "HMACSHA512" => new HMACSHA512(keyBytes).ComputeHash(textBytes),
            "HMACSHA384" => new HMACSHA384(keyBytes).ComputeHash(textBytes),
            "HMACSHA224" => HmacSha224(keyBytes, textBytes),
            _ => throw new ArgumentException($"Unsupported algorithm: {algorithmName}"),
        };

        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    // HMAC-SHA-224. NOTE: SHA-224 / HMACSHA224 are NOT available in the .NET 8 BCL
    // (they were only added in .NET 9), so we implement it manually.
    // SHA-224 is SHA-256 with different initial hash values, truncated to 224 bits.
    private static byte[] HmacSha224(byte[] key, byte[] message)
    {
        const int blockSize = 64;
        if (key.Length > blockSize)
        {
            key = Sha224(key);
        }

        if (key.Length < blockSize)
        {
            var padded = new byte[blockSize];
            Buffer.BlockCopy(key, 0, padded, 0, key.Length);
            key = padded;
        }

        var ipad = new byte[blockSize];
        var opad = new byte[blockSize];
        for (int i = 0; i < blockSize; i++)
        {
            ipad[i] = (byte)(key[i] ^ 0x36);
            opad[i] = (byte)(key[i] ^ 0x5c);
        }

        var inner = Sha224(Concat(ipad, message));
        return Sha224(Concat(opad, inner));
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var r = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, r, 0, a.Length);
        Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
        return r;
    }

    // SHA-224 (truncated SHA-256 with different initial hash values)
    private static byte[] Sha224(byte[] data)
    {
        uint[] h = { 0xc1059ed8, 0x367cd507, 0x3070dd17, 0xf70e5939, 0xffc00b31, 0x68581511, 0x64f98fa7, 0xbefa4fa4 };

        uint[] k =
        {
            0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
            0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
            0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
            0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
            0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
            0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
            0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
            0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
        };

        var bitLen = (ulong)data.Length * 8;
        var withPad = new List<byte>(data);
        withPad.Add(0x80);
        while (withPad.Count % 64 != 56)
        {
            withPad.Add(0x00);
        }

        for (int i = 7; i >= 0; i--)
        {
            withPad.Add((byte)(bitLen >> (8 * i)));
        }

        var bytes = withPad.ToArray();
        for (int chunk = 0; chunk < bytes.Length; chunk += 64)
        {
            var w = new uint[64];
            for (int i = 0; i < 16; i++)
            {
                w[i] = (uint)((bytes[chunk + i * 4] << 24) | (bytes[chunk + i * 4 + 1] << 16) | (bytes[chunk + i * 4 + 2] << 8) | bytes[chunk + i * 4 + 3]);
            }

            for (int i = 16; i < 64; i++)
            {
                uint s0 = Ror(w[i - 15], 7) ^ Ror(w[i - 15], 18) ^ (w[i - 15] >> 3);
                uint s1 = Ror(w[i - 2], 17) ^ Ror(w[i - 2], 19) ^ (w[i - 2] >> 10);
                w[i] = w[i - 16] + s0 + w[i - 7] + s1;
            }

            uint a = h[0], b = h[1], c = h[2], d = h[3], e = h[4], f = h[5], g = h[6], hh = h[7];
            for (int i = 0; i < 64; i++)
            {
                uint s1 = Ror(e, 6) ^ Ror(e, 11) ^ Ror(e, 25);
                uint ch = (e & f) ^ (~e & g);
                uint temp1 = hh + s1 + ch + k[i] + w[i];
                uint s0 = Ror(a, 2) ^ Ror(a, 13) ^ Ror(a, 22);
                uint maj = (a & b) ^ (a & c) ^ (b & c);
                uint temp2 = s0 + maj;
                hh = g;
                g = f;
                f = e;
                e = d + temp1;
                d = c;
                c = b;
                b = a;
                a = temp1 + temp2;
            }

            h[0] += a;
            h[1] += b;
            h[2] += c;
            h[3] += d;
            h[4] += e;
            h[5] += f;
            h[6] += g;
            h[7] += hh;
        }

        // SHA-224 produces the first 7 words (28 bytes)
        var output = new byte[28];
        for (int i = 0; i < 7; i++)
        {
            output[i * 4] = (byte)(h[i] >> 24);
            output[i * 4 + 1] = (byte)(h[i] >> 16);
            output[i * 4 + 2] = (byte)(h[i] >> 8);
            output[i * 4 + 3] = (byte)(h[i]);
        }

        return output;
    }

    private static uint Ror(uint x, int n) => (x >> n) | (x << (32 - n));
}
