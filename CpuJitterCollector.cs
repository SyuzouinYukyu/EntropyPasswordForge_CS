using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;

namespace EntropyPasswordForge_CS;

internal static class CpuJitterCollector
{
    public static byte[] Collect(TimeSpan duration)
    {
        Stopwatch limit = Stopwatch.StartNew();
        byte[] pool = RandomNumberGenerator.GetBytes(SecureMixer.PoolSize);
        byte[] sample = new byte[96];
        long previous = Stopwatch.GetTimestamp();
        int counter = 0;

        try
        {
            while (limit.Elapsed < duration)
            {
                long beforeSpin = Stopwatch.GetTimestamp();
                Thread.SpinWait(64 + (counter & 127));
                long afterSpin = Stopwatch.GetTimestamp();

                long beforeDelay = Stopwatch.GetTimestamp();
                Task.Delay(1).GetAwaiter().GetResult();
                long afterDelay = Stopwatch.GetTimestamp();

                BinaryPrimitives.WriteInt64LittleEndian(sample.AsSpan(0, 8), previous);
                BinaryPrimitives.WriteInt64LittleEndian(sample.AsSpan(8, 8), beforeSpin);
                BinaryPrimitives.WriteInt64LittleEndian(sample.AsSpan(16, 8), afterSpin - beforeSpin);
                BinaryPrimitives.WriteInt64LittleEndian(sample.AsSpan(24, 8), afterDelay - beforeDelay);
                BinaryPrimitives.WriteInt64LittleEndian(sample.AsSpan(32, 8), Stopwatch.GetTimestamp() - previous);
                BinaryPrimitives.WriteInt64LittleEndian(sample.AsSpan(40, 8), GC.GetTotalMemory(false));
                BinaryPrimitives.WriteInt64LittleEndian(sample.AsSpan(48, 8), Environment.WorkingSet);
                BinaryPrimitives.WriteInt64LittleEndian(sample.AsSpan(56, 8), Environment.TickCount64);
                BinaryPrimitives.WriteInt32LittleEndian(sample.AsSpan(64, 4), Environment.CurrentManagedThreadId);
                BinaryPrimitives.WriteInt32LittleEndian(sample.AsSpan(68, 4), counter++);
                RandomNumberGenerator.Fill(sample.AsSpan(72, 24));

                byte[] mixed = SecureMixer.Sha512(pool, sample);
                CryptographicOperations.ZeroMemory(pool);
                pool = mixed;
                previous = Stopwatch.GetTimestamp();
            }

            return pool;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sample);
        }
    }
}
