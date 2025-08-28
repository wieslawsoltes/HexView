using System.Text;

namespace HexView.Avalonia.Model;

public interface IHexFormatter
{
    long Length { get; }
    long Lines { get; }
    int Width { get; set; }
    int OffsetPadding { get; }
    void AddLine(byte[] bytes, long lineNumber, StringBuilder sb, int toBase);

    // Column configuration
    int GroupSize { get; set; }
    bool ShowGroupSeparator { get; set; }
    int AddressWidthOverride { get; set; }

    // ASCII pane configuration
    Encoding Encoding { get; set; }
    bool UseControlGlyph { get; set; }
    char ControlGlyph { get; set; }
}
