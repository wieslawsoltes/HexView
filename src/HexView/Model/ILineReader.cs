// HexView control for Avalonia.
// Copyright (C) 2025  Wiesław Šoltés

using System;

namespace HexView.Avalonia.Model;

/// <summary>
/// Reads bytes by logical offset and line width for the HexView rendering pipeline.
/// </summary>
public interface ILineReader : IDisposable
{
    byte[] GetLine(long lineNumber, int width);
    int Read(long offset, byte[] buffer, int count);
    long Length { get; }
}
