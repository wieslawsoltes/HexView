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
using HexView.Avalonia.Model;

namespace HexView.Avalonia.Services;

public enum EditMode
{
    Overwrite,
    Insert
}

internal struct Piece
{
    public bool IsInsert;
    public long Start;
    public long Length;
}

public class ByteOverlay
{
    private readonly List<Piece> _pieces = new();
    private readonly List<byte> _insertBuffer = new();
    private readonly SortedDictionary<long, byte> _overwrite = new();

    private readonly ILineReader _reader;

    public ByteOverlay(ILineReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _pieces.Add(new Piece { IsInsert = false, Start = 0, Length = reader.Length });
    }

    public EditMode Mode { get; set; } = EditMode.Overwrite;

    public long OriginalLength => _reader.Length;

    public long Length
    {
        get
        {
            if (Mode == EditMode.Overwrite)
                return _reader.Length;
            long sum = 0;
            foreach (var p in _pieces) sum += p.Length;
            return sum;
        }
    }

    public IEnumerable<long> GetOverwriteOffsets() => _overwrite.Keys;

    public IReadOnlyDictionary<long, byte> GetOverwriteEdits() => _overwrite;

    public int Read(long offset, byte[] buffer, int count)
    {
        if (buffer is null) throw new ArgumentNullException(nameof(buffer));
        if (count <= 0) return 0;
        if (offset < 0) return 0;

        if (Mode == EditMode.Overwrite)
        {
            var read = _reader.Read(offset, buffer, count);
            if (read <= 0) return read;
            // Apply overlay edits in-place
            foreach (var kv in _overwrite)
            {
                long pos = kv.Key;
                if (pos >= offset && pos < offset + read)
                {
                    buffer[pos - offset] = kv.Value;
                }
            }
            return read;
        }
        else
        {
            // Read through piece table
            long remaining = count;
            int written = 0;
            long logical = 0;
            foreach (var p in _pieces)
            {
                if (remaining <= 0) break;
                if (offset >= logical + p.Length)
                {
                    logical += p.Length; // skip
                    continue;
                }

                long localOffset = Math.Max(0, offset - logical);
                long canRead = Math.Min(remaining, p.Length - localOffset);
                if (canRead <= 0) break;

                if (!p.IsInsert)
                {
                    // From original reader
                    var tmp = new byte[canRead];
                    var read = _reader.Read(p.Start + localOffset, tmp, (int)canRead);
                    Array.Copy(tmp, 0, buffer, written, read);
                    written += read;
                    remaining -= read;
                }
                else
                {
                    // From insert buffer
                    for (int i = 0; i < canRead; i++)
                    {
                        buffer[written + i] = _insertBuffer[(int)(p.Start + localOffset + i)];
                    }
                    written += (int)canRead;
                    remaining -= canRead;
                }

                logical += p.Length;
                offset += canRead;
            }
            return written;
        }
    }

    // Enumerate insert operations as logical offsets with data
    public IEnumerable<(long Offset, byte[] Data)> GetInserts()
    {
        long logical = 0;
        foreach (var p in _pieces)
        {
            if (p.IsInsert)
            {
                var data = new byte[p.Length];
                for (int i = 0; i < p.Length; i++) data[i] = _insertBuffer[(int)(p.Start + i)];
                yield return (logical, data);
            }
            logical += p.Length;
        }
    }

    // Enumerate deletions as ranges in original coordinates
    public IEnumerable<(long Offset, long Length)> GetDeletions()
    {
        long origPos = 0;
        foreach (var p in _pieces)
        {
            if (!p.IsInsert)
            {
                if (p.Start > origPos)
                {
                    yield return (origPos, p.Start - origPos);
                }
                origPos = p.Start + p.Length;
            }
        }
        if (origPos < _reader.Length)
        {
            yield return (origPos, _reader.Length - origPos);
        }
    }

    public void OverwriteByte(long offset, byte value)
    {
        if (Mode == EditMode.Overwrite)
        {
            if (offset < 0 || offset >= _reader.Length) return;
            // If new value equals original, remove from overwrite map to clear highlight
            var orig = new byte[1];
            var read = _reader.Read(offset, orig, 1);
            if (read == 1 && orig[0] == value)
            {
                _overwrite.Remove(offset);
            }
            else
            {
                _overwrite[offset] = value;
            }
        }
        else
        {
            ReplaceRange(offset, 1, new[] { value });
        }
    }

    public void InsertBytes(long offset, byte[] bytes)
    {
        if (bytes is null || bytes.Length == 0) return;
        if (Mode == EditMode.Overwrite)
        {
            // Insert is allowed only in insert mode; switch or ignore based on preference
            Mode = EditMode.Insert;
        }
        InsertPiece(offset, bytes);
    }

    public void DeleteRange(long offset, long length)
    {
        if (Mode == EditMode.Overwrite)
        {
            return;
        }
        if (length <= 0) return;
        long logical = 0;
        for (int i = 0; i < _pieces.Count && length > 0; i++)
        {
            var p = _pieces[i];
            if (offset >= logical + p.Length)
            {
                logical += p.Length;
                continue;
            }
            long localOffset = Math.Max(0, offset - logical);
            long take = Math.Min(length, p.Length - localOffset);
            if (take == p.Length)
            {
                _pieces.RemoveAt(i);
                i--;
            }
            else
            {
                // split piece
                var left = new Piece { IsInsert = p.IsInsert, Start = p.Start, Length = localOffset };
                var right = new Piece { IsInsert = p.IsInsert, Start = p.Start + localOffset + take, Length = p.Length - localOffset - take };
                var newPieces = new List<Piece>();
                if (left.Length > 0) newPieces.Add(left);
                if (right.Length > 0) newPieces.Add(right);
                _pieces.RemoveAt(i);
                _pieces.InsertRange(i, newPieces);
                i += newPieces.Count - 1;
            }
            length -= take;
            logical += p.Length;
            offset += take;
        }
    }

    public void ReplaceRange(long offset, long length, byte[] bytes)
    {
        if (Mode == EditMode.Overwrite)
        {
            // Convert to per-byte overwrite for replaced span
            for (int i = 0; i < bytes.Length; i++)
            {
                OverwriteByte(offset + i, bytes[i]);
            }
            return;
        }
        DeleteRange(offset, length);
        InsertPiece(offset, bytes);
    }

    private void InsertPiece(long offset, byte[] bytes)
    {
        long insertStart = _insertBuffer.Count;
        _insertBuffer.AddRange(bytes);

        long logical = 0;
        for (int i = 0; i < _pieces.Count; i++)
        {
            var p = _pieces[i];
            if (offset > logical + p.Length)
            {
                logical += p.Length;
                continue;
            }
            long localOffset = Math.Max(0, offset - logical);
            if (localOffset == 0)
            {
                _pieces.Insert(i, new Piece { IsInsert = true, Start = insertStart, Length = bytes.Length });
                return;
            }
            if (localOffset == p.Length)
            {
                _pieces.Insert(i + 1, new Piece { IsInsert = true, Start = insertStart, Length = bytes.Length });
                return;
            }
            // split and insert in the middle
            var left = new Piece { IsInsert = p.IsInsert, Start = p.Start, Length = localOffset };
            var right = new Piece { IsInsert = p.IsInsert, Start = p.Start + localOffset, Length = p.Length - localOffset };
            _pieces.RemoveAt(i);
            _pieces.Insert(i, left);
            _pieces.Insert(i + 1, new Piece { IsInsert = true, Start = insertStart, Length = bytes.Length });
            _pieces.Insert(i + 2, right);
            return;
        }

        // append at end
        _pieces.Add(new Piece { IsInsert = true, Start = insertStart, Length = bytes.Length });
    }
}
