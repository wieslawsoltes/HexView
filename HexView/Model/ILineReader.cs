using System;

namespace HexView.Model;

public interface ILineReader : IDisposable
{
    byte[] GetLine(long lineNumber, int width);
}
