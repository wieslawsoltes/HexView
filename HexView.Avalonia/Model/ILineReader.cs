using System;

namespace HexView.Avalonia.Model;

public interface ILineReader : IDisposable
{
    byte[] GetLine(long lineNumber, int width);
}
