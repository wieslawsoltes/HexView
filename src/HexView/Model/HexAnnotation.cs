// HexView control for Avalonia.
// Copyright (C) 2025  Wiesław Šoltés

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace HexView.Avalonia.Model;

public class HexAnnotation : INotifyPropertyChanged
{
    private long _start;
    private long _length = 1;
    private string _label = string.Empty;
    private Color _color = Colors.DeepSkyBlue;
    private bool _isDraggable = true;

    public long Start
    {
        get => _start;
        set
        {
            if (SetProperty(ref _start, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(End));
            }
        }
    }

    public long Length
    {
        get => _length;
        set
        {
            if (SetProperty(ref _length, Math.Max(1, value)))
            {
                OnPropertyChanged(nameof(End));
            }
        }
    }

    public long End => Start + Length - 1;

    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value ?? string.Empty);
    }

    public Color Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    public bool IsDraggable
    {
        get => _isDraggable;
        set => SetProperty(ref _isDraggable, value);
    }

    public bool Contains(long offset)
    {
        return offset >= Start && offset <= End;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
