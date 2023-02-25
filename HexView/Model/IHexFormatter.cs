using System.Text;

namespace HexView.Model;

public interface IHexFormatter
{
    long Lines { get; }
    int Width { get; set; }
    void AddLine(byte[] bytes, long lineNumber, StringBuilder sb, int toBase);
}
