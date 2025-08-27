using System;
using System.Text;
using HexView.Avalonia.Model;

namespace HexView.Avalonia.Services;

public class HexFormatter : IHexFormatter
{
    private readonly long _length;
    private long _lines;
    private int _width;
    private readonly int _offsetPadding;

    public HexFormatter(long length)
    {
        _length = length;
        _width = 8; 
        _lines = (long)Math.Ceiling((decimal)_length / _width);
        _offsetPadding = _length.ToString("X").Length;
    }

    public long Length => _length;

    public long Lines => _lines;

    public int Width
    {
        get => _width;
        set
        {
            _width = value;
            _lines = (long)Math.Ceiling((decimal)_length / _width);
        }
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

            if (position < _length)
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

    public int OffsetPadding => _offsetPadding;
}
