using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HexView.Avalonia.Services;

public static class SaveService
{
    public static void SaveAs(ByteOverlay overlay, string path)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        var buffer = new byte[64 * 1024];
        long pos = 0;
        while (pos < overlay.Length)
        {
            int toRead = (int)Math.Min(buffer.Length, overlay.Length - pos);
            int read = overlay.Read(pos, buffer, toRead);
            if (read <= 0) break;
            fs.Write(buffer, 0, read);
            pos += read;
        }
        fs.Flush(true);
    }

    public static string ExportPatch(ByteOverlay overlay)
    {
        // Simple textual patch: OVERWRITE and INSERT entries
        var sb = new StringBuilder();
        foreach (var kv in overlay.GetOverwriteEdits())
        {
            sb.AppendLine($"OW 0x{kv.Key:X}: {kv.Value:X2}");
        }
        foreach (var ins in overlay.GetInserts())
        {
            sb.Append("IN 0x").Append(ins.Offset.ToString("X")).Append(':');
            for (int i = 0; i < ins.Data.Length; i++)
            {
                sb.Append(' ').Append(ins.Data[i].ToString("X2"));
            }
            sb.AppendLine();
        }
        foreach (var del in overlay.GetDeletions())
        {
            if (del.Length > 0)
            {
                sb.AppendLine($"DL 0x{del.Offset:X}: {del.Length}");
            }
        }
        return sb.ToString();
    }
}
