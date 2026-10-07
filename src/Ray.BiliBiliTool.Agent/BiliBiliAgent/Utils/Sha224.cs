using System.Buffers.Binary;
using System.Numerics;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;

// SHA-224 uses the SHA-256 compression function with its own IV and truncated output.
internal static class Sha224
{
    private static readonly uint[] Constants =
    [
        0x428a2f98,
        0x71374491,
        0xb5c0fbcf,
        0xe9b5dba5,
        0x3956c25b,
        0x59f111f1,
        0x923f82a4,
        0xab1c5ed5,
        0xd807aa98,
        0x12835b01,
        0x243185be,
        0x550c7dc3,
        0x72be5d74,
        0x80deb1fe,
        0x9bdc06a7,
        0xc19bf174,
        0xe49b69c1,
        0xefbe4786,
        0x0fc19dc6,
        0x240ca1cc,
        0x2de92c6f,
        0x4a7484aa,
        0x5cb0a9dc,
        0x76f988da,
        0x983e5152,
        0xa831c66d,
        0xb00327c8,
        0xbf597fc7,
        0xc6e00bf3,
        0xd5a79147,
        0x06ca6351,
        0x14292967,
        0x27b70a85,
        0x2e1b2138,
        0x4d2c6dfc,
        0x53380d13,
        0x650a7354,
        0x766a0abb,
        0x81c2c92e,
        0x92722c85,
        0xa2bfe8a1,
        0xa81a664b,
        0xc24b8b70,
        0xc76c51a3,
        0xd192e819,
        0xd6990624,
        0xf40e3585,
        0x106aa070,
        0x19a4c116,
        0x1e376c08,
        0x2748774c,
        0x34b0bcb5,
        0x391c0cb3,
        0x4ed8aa4a,
        0x5b9cca4f,
        0x682e6ff3,
        0x748f82ee,
        0x78a5636f,
        0x84c87814,
        0x8cc70208,
        0x90befffa,
        0xa4506ceb,
        0xbef9a3f7,
        0xc67178f2,
    ];

    public static byte[] Hmac(byte[] key, byte[] message)
    {
        if (key.Length > 64)
            key = Hash(key);
        var inner = new byte[64 + message.Length];
        var outer = new byte[64 + 28];
        for (var i = 0; i < 64; i++)
        {
            var value = i < key.Length ? key[i] : 0;
            inner[i] = (byte)(value ^ 0x36);
            outer[i] = (byte)(value ^ 0x5c);
        }
        message.CopyTo(inner, 64);
        Hash(inner).CopyTo(outer, 64);
        return Hash(outer);
    }

    private static byte[] Hash(byte[] message)
    {
        var padded = new byte[((message.Length + 9 + 63) / 64) * 64];
        message.CopyTo(padded, 0);
        padded[message.Length] = 0x80;
        BinaryPrimitives.WriteUInt64BigEndian(
            padded.AsSpan(padded.Length - 8),
            (ulong)message.Length * 8
        );
        uint[] state =
        [
            0xc1059ed8,
            0x367cd507,
            0x3070dd17,
            0xf70e5939,
            0xffc00b31,
            0x68581511,
            0x64f98fa7,
            0xbefa4fa4,
        ];
        var words = new uint[64];
        unchecked
        {
            for (var offset = 0; offset < padded.Length; offset += 64)
            {
                for (var i = 0; i < 16; i++)
                    words[i] = BinaryPrimitives.ReadUInt32BigEndian(padded.AsSpan(offset + i * 4));
                for (var i = 16; i < 64; i++)
                {
                    var x = words[i - 15];
                    var y = words[i - 2];
                    var s0 =
                        BitOperations.RotateRight(x, 7)
                        ^ BitOperations.RotateRight(x, 18)
                        ^ (x >> 3);
                    var s1 =
                        BitOperations.RotateRight(y, 17)
                        ^ BitOperations.RotateRight(y, 19)
                        ^ (y >> 10);
                    words[i] = words[i - 16] + s0 + words[i - 7] + s1;
                }
                var a = state[0];
                var b = state[1];
                var c = state[2];
                var d = state[3];
                var e = state[4];
                var f = state[5];
                var g = state[6];
                var h = state[7];
                for (var i = 0; i < 64; i++)
                {
                    var s1 =
                        BitOperations.RotateRight(e, 6)
                        ^ BitOperations.RotateRight(e, 11)
                        ^ BitOperations.RotateRight(e, 25);
                    var t1 = h + s1 + ((e & f) ^ (~e & g)) + Constants[i] + words[i];
                    var s0 =
                        BitOperations.RotateRight(a, 2)
                        ^ BitOperations.RotateRight(a, 13)
                        ^ BitOperations.RotateRight(a, 22);
                    var t2 = s0 + ((a & b) ^ (a & c) ^ (b & c));
                    h = g;
                    g = f;
                    f = e;
                    e = d + t1;
                    d = c;
                    c = b;
                    b = a;
                    a = t1 + t2;
                }
                state[0] += a;
                state[1] += b;
                state[2] += c;
                state[3] += d;
                state[4] += e;
                state[5] += f;
                state[6] += g;
                state[7] += h;
            }
        }
        var result = new byte[28];
        for (var i = 0; i < 7; i++)
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(i * 4), state[i]);
        return result;
    }
}
