using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace EntropyPasswordForge_CS;

internal static class SecureMixer
{
    public const int PoolSize = 64;

    public static byte[] Sha512(params byte[][] parts)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        foreach (byte[] part in parts)
        {
            hash.AppendData(part);
        }

        return hash.GetHashAndReset();
    }

    public static byte[] HmacSha512(byte[] key, params byte[][] parts)
    {
        using HMACSHA512 hmac = new HMACSHA512(key);
        foreach (byte[] part in parts)
        {
            hmac.TransformBlock(part, 0, part.Length, null, 0);
        }

        hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return hmac.Hash ?? throw new CryptographicException("HMAC-SHA512 failed.");
    }

    public static byte[] BuildFinalSeed(byte[] mousePool, byte[] cpuJitter)
    {
        byte[] osRandom = RandomNumberGenerator.GetBytes(64);
        byte[] meta = new byte[64];
        byte[] machineBytes = Encoding.UTF8.GetBytes(Environment.MachineName);

        try
        {
            BinaryPrimitives.WriteInt64LittleEndian(meta.AsSpan(0, 8), DateTimeOffset.UtcNow.Ticks);
            BinaryPrimitives.WriteInt32LittleEndian(meta.AsSpan(8, 4), Environment.ProcessId);
            BinaryPrimitives.WriteInt32LittleEndian(meta.AsSpan(12, 4), Environment.CurrentManagedThreadId);
            BinaryPrimitives.WriteInt64LittleEndian(meta.AsSpan(16, 8), Stopwatch.GetTimestamp());
            BinaryPrimitives.WriteInt64LittleEndian(meta.AsSpan(24, 8), Environment.TickCount64);
            RandomNumberGenerator.Fill(meta.AsSpan(32, 32));

            byte[] preSeed = Sha512(osRandom, mousePool, cpuJitter, meta, machineBytes);
            byte[] key = HmacSha512(osRandom, preSeed, meta);
            byte[] state = HmacSha512(key, mousePool, cpuJitter, osRandom);
            byte[] seed = new byte[128];
            Buffer.BlockCopy(key, 0, seed, 0, 64);
            Buffer.BlockCopy(state, 0, seed, 64, 64);
            CryptographicOperations.ZeroMemory(preSeed);
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(state);
            return seed;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(osRandom);
            CryptographicOperations.ZeroMemory(meta);
            CryptographicOperations.ZeroMemory(machineBytes);
        }
    }
}
