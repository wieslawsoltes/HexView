using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace HexView.Controls;

public class HexViewState : IDisposable
{
    private readonly FileStream _stream;
    private readonly MemoryMappedFile _file;
    private readonly MemoryMappedViewAccessor _accessor;
    private long _lines;
    private int _width;
    private readonly int _offsetPadding;

    public long Lines => _lines;

    public int Width
    {
        get => _width;
        set
        {
            _width = value;
            _lines = (long)Math.Ceiling((decimal)_stream.Length / _width);
        }
    }

    public HexViewState(string path) :
        this(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    {
    }

    public HexViewState(FileStream stream)
    {
        _stream = stream;
        _file = MemoryMappedFile.CreateFromFile(
            _stream,
            null, 
            0, 
            MemoryMappedFileAccess.Read,
            HandleInheritability.None,
            false); 
        _accessor = _file.CreateViewAccessor(0, _stream.Length, MemoryMappedFileAccess.Read);
        _width = 8; 
        _lines = (long)Math.Ceiling((decimal)_stream.Length / _width);
        _offsetPadding = _stream.Length.ToString("X").Length;
    }
    
    public byte[] GetLine(long lineNumber)
    {
        var width = _width;
        var bytes = new byte[width];
        var offset = lineNumber * width;

        for (var j = 0; j < width; j++)
        {
            var position = offset + j;
            if (position < _stream.Length)
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

    public void AddLine(byte[] bytes, long lineNumber, StringBuilder sb, int toBase)
    {
        if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
        {
            throw new ArgumentException("Invalid base");
        }

        var width = _width;
        var offset = lineNumber * width;

        sb.Append($"{offset.ToString($"X{_offsetPadding}")}: ");

        var toBasePadding = toBase switch
        {
            2 => 8,
            8 => 3,
            10 => 3,
            16 => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(toBase), toBase, null)
        };

        var paddingChar = toBase switch
        {
            2 => '0',
            8 => ' ',
            10 => ' ',
            16 => '0',
            _ => throw new ArgumentOutOfRangeException(nameof(toBase), toBase, null)
        };

        for (var j = 0; j < width; j++)
        {
            var position = offset + j;

            var isSplit = j > 0 && j % 8 == 0;
            if (isSplit)
            {
                sb.Append("| ");
            }

            if (position < _stream.Length)
            {
                if (toBase == 16)
                {
                    var value = $"{bytes[j]:X2}";
                    sb.Append(value);
                }
                else
                {
                    var value = Convert.ToString(bytes[j], toBase).PadLeft(toBasePadding, paddingChar);
                    sb.Append(value);
                }
            }
            else
            {
                var value = new string(' ', toBasePadding);
                sb.Append(value);
            }

            sb.Append(' ');
        }

        sb.Append(" | ");

        for (var j = 0; j < width; j++)
        {
            var c = (char)bytes[j];

            sb.Append(char.IsControl(c) ? ' ' : c);
        }
    }

    public void Dispose()
    {
        _accessor.Dispose();
        _file.Dispose();
        _stream.Dispose();
    }
}
