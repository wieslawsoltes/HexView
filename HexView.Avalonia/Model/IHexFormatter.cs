using System.Text;

namespace HexView.Avalonia.Model;

public interface IHexFormatter
{
    long Length { get; }
    long Lines { get; }
    int Width { get; set; }
    int OffsetPadding { get; }
    void AddLine(byte[] bytes, long lineNumber, StringBuilder sb, int toBase);
}
