using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;

namespace EntropyPasswordForge_CS;

internal sealed class MouseEntropyCollector
{
    private readonly object _gate = new();
    private byte[] _pool = RandomNumberGenerator.GetBytes(SecureMixer.PoolSize);
    private long _lastTimestamp;
    private Point? _lastPanelPoint;

    public long EventCount { get; private set; }

    public void Reset()
    {
        lock (_gate)
        {
            CryptographicOperations.ZeroMemory(_pool);
            _pool = RandomNumberGenerator.GetBytes(SecureMixer.PoolSize);
            _lastTimestamp = 0;
            _lastPanelPoint = null;
            EventCount = 0;
        }
    }

    public byte[] SnapshotPool()
    {
        lock (_gate)
        {
            return (byte[])_pool.Clone();
        }
    }

    public void Collect(MouseEventArgs e, Control panel, Form form, string eventKind)
    {
        long now = Stopwatch.GetTimestamp();
        long previous = Interlocked.Read(ref _lastTimestamp);
        long interval = previous == 0 ? 0 : now - previous;
        Interlocked.Exchange(ref _lastTimestamp, now);

        Point panelPoint = e.Location;
        Point formPoint = form.PointToClient(panel.PointToScreen(panelPoint));
        Point cursor = Cursor.Position;
        Point lastPoint = _lastPanelPoint ?? panelPoint;
        int dx = panelPoint.X - lastPoint.X;
        int dy = panelPoint.Y - lastPoint.Y;
        _lastPanelPoint = panelPoint;

        byte[] eventBytes = new byte[128];
        byte[] osRandom = RandomNumberGenerator.GetBytes(32);

        try
        {
            BinaryPrimitives.WriteInt64LittleEndian(eventBytes.AsSpan(0, 8), now);
            BinaryPrimitives.WriteInt64LittleEndian(eventBytes.AsSpan(8, 8), Environment.TickCount64);
            BinaryPrimitives.WriteInt64LittleEndian(eventBytes.AsSpan(16, 8), interval);
            BinaryPrimitives.WriteInt32LittleEndian(eventBytes.AsSpan(24, 4), panelPoint.X);
            BinaryPrimitives.WriteInt32LittleEndian(eventBytes.AsSpan(28, 4), panelPoint.Y);
            BinaryPrimitives.WriteInt32LittleEndian(eventBytes.AsSpan(32, 4), dx);
            BinaryPrimitives.WriteInt32LittleEndian(eventBytes.AsSpan(36, 4), dy);
            BinaryPrimitives.WriteInt32LittleEndian(eventBytes.AsSpan(40, 4), formPoint.X);
            BinaryPrimitives.WriteInt32LittleEndian(eventBytes.AsSpan(44, 4), formPoint.Y);
            BinaryPrimitives.WriteInt32LittleEndian(eventBytes.AsSpan(48, 4), cursor.X);
            BinaryPrimitives.WriteInt32LittleEndian(eventBytes.AsSpan(52, 4), cursor.Y);
            BinaryPrimitives.WriteInt32LittleEndian(eventBytes.AsSpan(56, 4), (int)e.Button);
            BinaryPrimitives.WriteInt32LittleEndian(eventBytes.AsSpan(60, 4), e.Delta);
            BinaryPrimitives.WriteInt64LittleEndian(eventBytes.AsSpan(64, 8), EventCount);
            BinaryPrimitives.WriteInt32LittleEndian(eventBytes.AsSpan(72, 4), eventKind.GetHashCode(StringComparison.Ordinal));

            lock (_gate)
            {
                byte[] highResolution = new byte[16];
                BinaryPrimitives.WriteInt64LittleEndian(highResolution.AsSpan(0, 8), Stopwatch.GetTimestamp());
                BinaryPrimitives.WriteInt64LittleEndian(highResolution.AsSpan(8, 8), Environment.TickCount64);

                byte[] mixed = SecureMixer.Sha512(_pool, eventBytes, osRandom, highResolution);
                CryptographicOperations.ZeroMemory(_pool);
                _pool = mixed;
                EventCount++;
                CryptographicOperations.ZeroMemory(highResolution);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(eventBytes);
            CryptographicOperations.ZeroMemory(osRandom);
        }
    }
}
