// HexView control for Avalonia.
// Copyright (C) 2025  Wiesław Šoltés

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HexView.Avalonia.Model;
using Avalonia.Media;

namespace HexView.Avalonia.Services;

public static class HexAnnotationStorage
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true
    };

    public static IReadOnlyList<HexAnnotation> Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return Array.Empty<HexAnnotation>();
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<HexAnnotation>();
        }

        var data = JsonSerializer.Deserialize<List<SerializableHexAnnotation>>(json, s_options);
        if (data is null || data.Count == 0)
        {
            return Array.Empty<HexAnnotation>();
        }

        return data
            .Select(ToModel)
            .ToList();
    }

    public static void Save(string path, IEnumerable<HexAnnotation> annotations)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        if (annotations is null)
        {
            throw new ArgumentNullException(nameof(annotations));
        }

        var data = annotations
            .Select(ToSerializable)
            .ToArray();

        var json = JsonSerializer.Serialize(data, s_options);
        File.WriteAllText(path, json);
    }

    private static HexAnnotation ToModel(SerializableHexAnnotation annotation)
    {
        return new HexAnnotation
        {
            Start = Math.Max(0, annotation.Start),
            Length = Math.Max(1, annotation.Length),
            Label = annotation.Label ?? string.Empty,
            Color = FromArgb(annotation.ColorArgb),
            IsDraggable = annotation.IsDraggable
        };
    }

    private static SerializableHexAnnotation ToSerializable(HexAnnotation annotation)
    {
        return new SerializableHexAnnotation
        {
            Start = annotation.Start,
            Length = annotation.Length,
            Label = annotation.Label,
            ColorArgb = ToArgb(annotation.Color),
            IsDraggable = annotation.IsDraggable
        };
    }

    private static uint ToArgb(Color color)
    {
        return ((uint)color.A << 24)
             | ((uint)color.R << 16)
             | ((uint)color.G << 8)
             | color.B;
    }

    private static Color FromArgb(uint value)
    {
        return Color.FromArgb(
            (byte)((value >> 24) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF));
    }

    private sealed class SerializableHexAnnotation
    {
        public long Start { get; set; }
        public long Length { get; set; }
        public string? Label { get; set; }
        public uint ColorArgb { get; set; }
        public bool IsDraggable { get; set; } = true;
    }
}
