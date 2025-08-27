using System;

namespace HexView.Avalonia.Model;

public interface ILineReader : IDisposable
{
    byte[] GetLine(long lineNumber, int width);
    int Read(long offset, byte[] buffer, int count);
}
