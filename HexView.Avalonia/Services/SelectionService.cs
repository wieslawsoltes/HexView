using System;
using System.Linq;
using System.Text;

namespace HexView.Avalonia.Services;

public class SelectionService
{
    private readonly ByteOverlay _overlay;

    public SelectionService(ByteOverlay overlay)
    {
        _overlay = overlay;
    }

    public long Start { get; private set; }
    public long Length { get; private set; }
    public bool HasSelection => Length > 0;

    public void Set(long start, long length)
    {
        if (start < 0) start = 0;
        Start = start;
        Length = Math.Max(0, length);
    }

    public void Clear()
    {
        Start = 0; Length = 0;
    }

    public void Fill(byte value)
    {
        if (!HasSelection) return;
        var bytes = Enumerable.Repeat(value, (int)Length).Select(b => (byte)b).ToArray();
        _overlay.ReplaceRange(Start, Length, bytes);
    }

    public void Zero() => Fill(0x00);

    public void Increment()
    {
        if (!HasSelection) return;
        var buffer = new byte[Length];
        _overlay.Read(Start, buffer, buffer.Length);
        for (int i = 0; i < buffer.Length; i++) buffer[i]++;
        _overlay.ReplaceRange(Start, Length, buffer);
    }

    public void Randomize(int? seed = null)
    {
        if (!HasSelection) return;
        var rnd = seed.HasValue ? new Random(seed.Value) : new Random();
        var bytes = new byte[Length];
        rnd.NextBytes(bytes);
        _overlay.ReplaceRange(Start, Length, bytes);
    }

    public string CopyHex(bool spaced = true)
    {
        if (!HasSelection) return string.Empty;
        var buffer = new byte[Length];
        _overlay.Read(Start, buffer, buffer.Length);
        var sb = new StringBuilder(buffer.Length * 2);
        for (int i = 0; i < buffer.Length; i++)
        {
            if (spaced && i > 0) sb.Append(' ');
            sb.Append(buffer[i].ToString("X2"));
        }
        return sb.ToString();
    }

    public string CopyAscii()
    {
        if (!HasSelection) return string.Empty;
        var buffer = new byte[Length];
        _overlay.Read(Start, buffer, buffer.Length);
        var sb = new StringBuilder(buffer.Length);
        foreach (var b in buffer)
        {
            var c = (char)b;
            sb.Append(char.IsControl(c) ? ' ' : c);
        }
        return sb.ToString();
    }

    public void PasteHex(string hex, bool insert)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;
        var cleaned = new string(hex.Where(Uri.IsHexDigit).ToArray());
        if (cleaned.Length % 2 != 0) return;
        var data = new byte[cleaned.Length / 2];
        for (int i = 0; i < data.Length; i++) data[i] = Convert.ToByte(cleaned.Substring(i * 2, 2), 16);
        if (insert)
        {
            _overlay.InsertBytes(Start, data);
        }
        else
        {
            _overlay.ReplaceRange(Start, Length, data);
        }
    }

    public void PasteAscii(string text, Encoding? encoding = null, bool insert = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        var enc = encoding ?? Encoding.ASCII;
        var data = enc.GetBytes(text);
        if (insert)
        {
            _overlay.InsertBytes(Start, data);
        }
        else
        {
            _overlay.ReplaceRange(Start, Length, data);
        }
    }
}

