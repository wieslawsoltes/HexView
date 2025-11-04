// HexView control for Avalonia.
// Copyright (C) 2025  Wiesław Šoltés

using System;
using System.Collections.Generic;

namespace HexView.Avalonia.Services;

public class NavigationService
{
    private readonly ByteOverlay _overlay;
    private readonly Stack<long> _back = new();
    private readonly Stack<long> _forward = new();
    private readonly SortedSet<long> _bookmarks = new();

    public NavigationService(ByteOverlay overlay)
    {
        _overlay = overlay;
    }

    public long Current { get; private set; }

    public void Visit(long offset)
    {
        if (_back.Count == 0 || _back.Peek() != Current)
        {
            _back.Push(Current);
        }
        Current = Clamp(offset);
        _forward.Clear();
    }

    public bool CanBack => _back.Count > 0;
    public bool CanForward => _forward.Count > 0;

    public long Back()
    {
        if (!CanBack) return Current;
        _forward.Push(Current);
        Current = _back.Pop();
        return Current;
    }

    public long Forward()
    {
        if (!CanForward) return Current;
        _back.Push(Current);
        Current = _forward.Pop();
        return Current;
    }

    public void AddBookmark(long offset) => _bookmarks.Add(Clamp(offset));
    public void RemoveBookmark(long offset) => _bookmarks.Remove(Clamp(offset));
    public IEnumerable<long> GetBookmarks() => _bookmarks;

    public long NextChange(long from)
    {
        long start = Clamp(from + 1);
        long best = -1;
        foreach (var o in _overlay.GetOverwriteOffsets())
        {
            if (o >= start && (best < 0 || o < best)) best = o;
        }
        return best;
    }

    private long Clamp(long offset)
    {
        var max = Math.Max(0, _overlay.Length - 1);
        if (offset < 0) return 0;
        if (offset > max) return max;
        return offset;
    }
}

