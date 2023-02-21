using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace HexView;

public class HexViewState : IDisposable
{
    private FileInfo _info;
    private MemoryMappedFile? _file;
    private MemoryMappedViewAccessor _accessor;
    private int _width;
    private long _lines;

    public long Lines => _lines;
    
    public HexViewState(string path)
    {
        _info = new FileInfo(path); 
        _file = MemoryMappedFile.CreateFromFile(path); 
        _accessor = _file.CreateViewAccessor(0, _info.Length);
        _width = 16; // 8, 16, 24 or 32
        _lines = (long)Math.Ceiling((decimal)_info.Length / _width);
    }

    public byte[] GetLine(long lineNumber)
    {
        var bytes = new byte[_width];
        var offset = lineNumber * _width;

        for (var j = 0; j < _width; j++)
        {
            var position = offset + j;
            if (position < _info.Length)
            {
                bytes[j] = _accessor.ReadByte(position);
            }
            else
            {
                break;
            }
        }

        return bytes;
    }

    public void AddLine(byte[] bytes, long lineNumber, StringBuilder sb)
    {
        var offset = lineNumber * _width;

        sb.Append($"{offset:X10}: ");

        for (var j = 0; j < _width; j++)
        {
            var position = offset + j;

            var isSplit = j > 0 && j % 8 == 0;
            if (isSplit)
            {
                sb.Append("| ");
            }

            if (position < _info.Length)
            {
                sb.Append($"{bytes[j]:X2}");
            }
            else
            {
                sb.Append("  ");
            }

            if (!isSplit)
            {
                sb.Append(' ');
            }
        }

        sb.Append(" | ");

        for (var j = 0; j < _width; j++)
        {
            var c = (char)bytes[j];

            sb.Append(char.IsControl(c) ? ' ' : c);
        }
    }

    public void Dispose()
    {
        _accessor.Dispose();
        _file?.Dispose();
    }
}
