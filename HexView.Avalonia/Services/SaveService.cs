// HexView control for Avalonia.
// Copyright (C) 2025  Wiesław Šoltés
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
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
