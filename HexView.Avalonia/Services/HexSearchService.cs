using System;
using System.Linq;
using HexView.Avalonia.Model;

namespace HexView.Avalonia.Services;

public static class HexSearchService
{
    public static bool TryParseAddress(string? text, out long address)
    {
        address = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return long.TryParse(text.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out address);
        }
        // If any hex letter present, parse as hex
        if (text.IndexOfAny(new[] { 'A','B','C','D','E','F','a','b','c','d','e','f' }) >= 0)
        {
            return long.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out address);
        }
        return long.TryParse(text, out address);
    }

    public static bool TryParseHexBytes(string? text, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim();
        // Keep only hex digits
        var cleaned = new string(text.Where(Uri.IsHexDigit).ToArray());
        if (cleaned.Length < 2 || cleaned.Length % 2 != 0) return false;
        var result = new byte[cleaned.Length / 2];
        for (int i = 0; i < result.Length; i++)
        {
            var s = cleaned.Substring(i * 2, 2);
            if (!byte.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out result[i]))
            {
                return false;
            }
        }
        bytes = result;
        return bytes.Length > 0;
    }

    public static long? FindNextValue(ILineReader reader, long length, byte[] pattern, long startOffset)
    {
        if (pattern is null || pattern.Length == 0 || length <= 0) return null;
        const int chunkSize = 64 * 1024;
        var buffer = new byte[Math.Max(chunkSize, pattern.Length * 2)];
        long pos = startOffset;
        bool wrapped = false;

        while (true)
        {
            var read = reader.Read(pos, buffer, buffer.Length);
            if (read <= 0)
            {
                if (wrapped) break;
                pos = 0; wrapped = true; continue;
            }
            var idx = IndexOf(buffer, read, pattern);
            if (idx >= 0)
            {
                return pos + idx;
            }
            pos += Math.Max(1, read - (pattern.Length - 1));
            if (pos >= length)
            {
                if (wrapped) break;
                pos = 0; wrapped = true;
            }
        }
        return null;
    }

    public static long? FindPrevValue(ILineReader reader, long length, byte[] pattern, long startOffset)
    {
        if (pattern is null || pattern.Length == 0 || length <= 0) return null;
        const int chunkSize = 64 * 1024;
        var buffer = new byte[Math.Max(chunkSize, pattern.Length * 2)];
        long pos = Math.Max(0, startOffset - buffer.Length);
        bool wrapped = false;

        while (true)
        {
            var toRead = (int)Math.Min(buffer.Length, startOffset - pos + 1);
            if (toRead <= 0)
            {
                if (wrapped) break;
                startOffset = length - 1; pos = Math.Max(0, startOffset - buffer.Length); wrapped = true; continue;
            }
            var read = reader.Read(pos, buffer, toRead);
            if (read <= 0)
            {
                if (wrapped) break;
                startOffset = length - 1; pos = Math.Max(0, startOffset - buffer.Length); wrapped = true; continue;
            }
            var idx = LastIndexOf(buffer, read, pattern);
            if (idx >= 0)
            {
                return pos + idx;
            }
            if (pos == 0)
            {
                if (wrapped) break;
                startOffset = length - 1; pos = Math.Max(0, startOffset - buffer.Length); wrapped = true; continue;
            }
            var step = Math.Max(1, read - (pattern.Length - 1));
            startOffset = pos - 1;
            pos = Math.Max(0, pos - step);
        }
        return null;
    }

    private static int IndexOf(byte[] buffer, int count, byte[] pattern)
    {
        if (pattern.Length == 0 || count < pattern.Length) return -1;
        int lastStart = count - pattern.Length;
        for (int i = 0; i <= lastStart; i++)
        {
            int j = 0; for (; j < pattern.Length; j++) if (buffer[i + j] != pattern[j]) break; if (j == pattern.Length) return i;
        }
        return -1;
    }

    private static int LastIndexOf(byte[] buffer, int count, byte[] pattern)
    {
        if (pattern.Length == 0 || count < pattern.Length) return -1;
        for (int i = count - pattern.Length; i >= 0; i--)
        {
            int j = 0; for (; j < pattern.Length; j++) if (buffer[i + j] != pattern[j]) break; if (j == pattern.Length) return i;
        }
        return -1;
    }
}
