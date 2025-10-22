﻿// HexView control for Avalonia.
// Copyright (C) 2025  Wiesław Šoltés

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using HexView.Avalonia.Model;

namespace HexView.Avalonia.Services;

public class MemoryMappedLineReader : ILineReader
{
    private readonly FileStream _stream;
    private readonly MemoryMappedFile _file;
    private readonly MemoryMappedViewAccessor _accessor;

    public MemoryMappedLineReader(string path) :
        this(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    {
    }

    public MemoryMappedLineReader(FileStream stream)
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

    }
    
    public byte[] GetLine(long lineNumber, int width)
    {
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

    public void Dispose()
    {
        _accessor.Dispose();
        _file.Dispose();
        _stream.Dispose();
    }

    public int Read(long offset, byte[] buffer, int count)
    {
        if (offset < 0 || offset >= _stream.Length || count <= 0)
        {
            return 0;
        }

        var max = (int)Math.Min(count, _stream.Length - offset);
        for (var i = 0; i < max; i++)
        {
            buffer[i] = _accessor.ReadByte(offset + i);
        }
        return max;
    }

    public long Length => _stream.Length;
}
