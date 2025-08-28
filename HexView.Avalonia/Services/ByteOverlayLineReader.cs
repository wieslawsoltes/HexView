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
using HexView.Avalonia.Model;

namespace HexView.Avalonia.Services;

public class ByteOverlayLineReader : ILineReader
{
    private readonly ByteOverlay _overlay;

    public ByteOverlayLineReader(ByteOverlay overlay)
    {
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
    }

    public byte[] GetLine(long lineNumber, int width)
    {
        var buffer = new byte[width];
        var offset = lineNumber * width;
        var read = Read(offset, buffer, width);
        if (read < width)
        {
            // zero-fill remainder
            for (int i = read; i < width; i++) buffer[i] = 0;
        }
        return buffer;
    }

    public int Read(long offset, byte[] buffer, int count)
    {
        return _overlay.Read(offset, buffer, count);
    }

    public long Length => _overlay.Length;

    public void Dispose()
    {
        // no-op; overlay owned by consumer
    }
}

