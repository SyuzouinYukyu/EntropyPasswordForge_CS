using System.Buffers.Binary;
using System.Security.Cryptography;

namespace EntropyPasswordForge_CS;

internal sealed class HmacSha512Drbg : IDisposable
{
    private readonly byte[] _key = new byte[64];
    private readonly byte[] _v = new byte[64];
    private ulong _counter;
    private bool _disposed;

    public HmacSha512Drbg(byte[] seed)
    {
        if (seed.Length < 128)
        {
            throw new ArgumentException("Seed must be at least 128 bytes.", nameof(seed));
        }

        Buffer.BlockCopy(seed, 0, _key, 0, 64);
        Buffer.BlockCopy(seed, 64, _v, 0, 64);
        Update(seed);
    }

    public byte[] GenerateBytes(int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        byte[] output = new byte[count];
        int offset = 0;
        byte[] counterBytes = new byte[8];

        try
        {
            while (offset < count)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(counterBytes, ++_counter);
                byte[] block = Hmac(_key, _v, counterBytes);
                Buffer.BlockCopy(block, 0, _v, 0, 64);

                int take = Math.Min(block.Length, count - offset);
                Buffer.BlockCopy(block, 0, output, offset, take);
                offset += take;
                CryptographicOperations.ZeroMemory(block);
            }

            Update(counterBytes);
            return output;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(counterBytes);
        }
    }

    public int NextInt32(int exclusiveUpperBound)
    {
        if (exclusiveUpperBound <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveUpperBound));
        }

        uint bound = (uint)exclusiveUpperBound;
        uint limit = uint.MaxValue - (uint.MaxValue % bound);

        while (true)
        {
            byte[] bytes = GenerateBytes(4);
            try
            {
                uint value = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
                if (value < limit)
                {
                    return (int)(value % bound);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(bytes);
            }
        }
    }

    private void Update(byte[] providedData)
    {
        byte[] zero = [0x00];
        byte[] one = [0x01];
        byte[] newKey = Hmac(_key, _v, zero, providedData);
        Buffer.BlockCopy(newKey, 0, _key, 0, 64);
        CryptographicOperations.ZeroMemory(newKey);

        byte[] newV = Hmac(_key, _v);
        Buffer.BlockCopy(newV, 0, _v, 0, 64);
        CryptographicOperations.ZeroMemory(newV);

        if (providedData.Length > 0)
        {
            newKey = Hmac(_key, _v, one, providedData);
            Buffer.BlockCopy(newKey, 0, _key, 0, 64);
            CryptographicOperations.ZeroMemory(newKey);

            newV = Hmac(_key, _v);
            Buffer.BlockCopy(newV, 0, _v, 0, 64);
            CryptographicOperations.ZeroMemory(newV);
        }

        CryptographicOperations.ZeroMemory(zero);
        CryptographicOperations.ZeroMemory(one);
    }

    private static byte[] Hmac(byte[] key, params byte[][] parts)
    {
        using HMACSHA512 hmac = new(key);
        foreach (byte[] part in parts)
        {
            hmac.TransformBlock(part, 0, part.Length, null, 0);
        }

        hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return hmac.Hash ?? throw new CryptographicException("HMAC-SHA512 failed.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_key);
        CryptographicOperations.ZeroMemory(_v);
        _disposed = true;
    }
}
