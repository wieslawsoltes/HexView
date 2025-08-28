using System;
using HexView.Avalonia.Model;

namespace HexView.Avalonia.Services;

public class ByteOverlayLineReader : ILineReader
{
    private readonly ByteOverlay _overlay;

    public ByteOverlayLineReader(ByteOverlay overlay)
    {
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
    }

    public byte[] GetLine(long lineNumber, int width)
    {
        var buffer = new byte[width];
        var offset = lineNumber * width;
        var read = Read(offset, buffer, width);
        if (read < width)
        {
            // zero-fill remainder
            for (int i = read; i < width; i++) buffer[i] = 0;
        }
        return buffer;
    }

    public int Read(long offset, byte[] buffer, int count)
    {
        return _overlay.Read(offset, buffer, count);
    }

    public long Length => _overlay.Length;

    public void Dispose()
    {
        // no-op; overlay owned by consumer
    }
}

