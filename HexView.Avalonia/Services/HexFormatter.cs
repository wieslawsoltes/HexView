// HexView control for Avalonia.
// Copyright (C) 2025  Wiesław Šoltés

using System;
using System.Text;
using HexView.Avalonia.Model;

namespace HexView.Avalonia.Services;

public class HexFormatter : IHexFormatter
{
    private readonly long _length;
    private long _lines;
    private int _width;
    private int _groupSize = 8;
    private bool _showGroupSeparator = true;
    private int _addressWidthOverride = 0;
    private Encoding _encoding = Encoding.ASCII;
    private bool _useControlGlyph = true;
    private char _controlGlyph = '·';

    public HexFormatter(long length)
    {
        _length = length;
        _width = 8; 
        _lines = (long)Math.Ceiling((decimal)_length / _width);
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

        var pad = OffsetPadding;
        sb.Append($"{offset.ToString($"X{pad}")}: ");

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

            if (_showGroupSeparator)
            {
                var isSplit = j > 0 && j % Math.Max(1, _groupSize) == 0;
                if (isSplit)
                {
                    sb.Append("| ");
                }
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
            char c;
            if (_encoding is { })
            {
                var s = _encoding.GetString(new[] { bytes[j] });
                c = string.IsNullOrEmpty(s) ? ' ' : s[0];
            }
            else
            {
                c = (char)bytes[j];
            }

            if (char.IsControl(c) && _useControlGlyph)
            {
                c = _controlGlyph;
            }

            sb.Append(c);
        }
    }

    public int OffsetPadding => _addressWidthOverride > 0 ? _addressWidthOverride : _length.ToString("X").Length;

    public int GroupSize { get => _groupSize; set => _groupSize = Math.Max(1, value); }
    public bool ShowGroupSeparator { get => _showGroupSeparator; set => _showGroupSeparator = value; }
    public int AddressWidthOverride { get => _addressWidthOverride; set => _addressWidthOverride = Math.Max(0, value); }
    public Encoding Encoding { get => _encoding; set => _encoding = value ?? Encoding.ASCII; }
    public bool UseControlGlyph { get => _useControlGlyph; set => _useControlGlyph = value; }
    public char ControlGlyph { get => _controlGlyph; set => _controlGlyph = value; }
}
