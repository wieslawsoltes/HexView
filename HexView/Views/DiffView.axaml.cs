﻿// HexView control for Avalonia.
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
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using HexView.Avalonia.Model;
using HexView.Avalonia.Services;

namespace HexView.Views;

public partial class DiffView : UserControl
{
    private ILineReader? _lineReader1;
    private IHexFormatter? _hexFormatter1;
    private ILineReader? _lineReader2;
    private IHexFormatter? _hexFormatter2;
    private bool _updating;

    public DiffView()
    {
        InitializeComponent();

        HexViewControl1.AddHandler(DragDrop.DropEvent, Drop);
        HexViewControl1.AddHandler(DragDrop.DragOverEvent, DragOver);
 
        HexViewControl2.AddHandler(DragDrop.DropEvent, Drop);
        HexViewControl2.AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private void OpenFile1(FileStream stream, string path)
    {
        _lineReader1?.Dispose();
        _lineReader1 = new MemoryMappedLineReader(stream);
        _hexFormatter1 = new HexFormatter(stream.Length);
        HexViewControl1.LineReader = _lineReader1;
        HexViewControl1.HexFormatter = _hexFormatter1;
        HexViewControl1.InvalidateScrollable();
        // TODO: path
    }

    private void OpenFile2(FileStream stream, string path)
    {
        _lineReader2?.Dispose();
        _lineReader2 = new MemoryMappedLineReader(stream);
        _hexFormatter2 = new HexFormatter(stream.Length);
        HexViewControl2.LineReader = _lineReader2;
        HexViewControl2.HexFormatter = _hexFormatter2;
        HexViewControl2.InvalidateScrollable();
        // TODO: path
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects &= (DragDropEffects.Copy | DragDropEffects.Link);

        if (!e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var path = e.Data.GetFileNames()?.FirstOrDefault();
            if (path is { })
            {
                if (Equals(sender, HexViewControl1))
                {
                    var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    OpenFile1(stream, path);
                }

                if (Equals(sender, HexViewControl2))
                {
                    var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    OpenFile2(stream, path);
                }
            }
        }
    }

    protected override void OnLoaded(RoutedEventArgs routedEventArgs)
    {
        base.OnLoaded(routedEventArgs);
#if DEBUG
        //var path = @"/Users/wieslawsoltes/Documents/GitHub/Acdparser/clippitMS/CLIPPIT.ACS";
        var path = @"c:\Users\Administrator\Documents\GitHub\Acdparser\clippitMS\CLIPPIT.ACS";

        var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        OpenFile1(stream, path);

        OpenFile2(stream, path);

        ScrollViewer1.ScrollChanged += ScrollViewer1OnScrollChanged;
        ScrollViewer2.ScrollChanged += ScrollViewer2OnScrollChanged;
#endif
    }

    private void ScrollViewer1OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_updating)
        {
            return;
        }

        _updating = true;
        ScrollViewer2.Offset = ScrollViewer1.Offset;
        _updating = false;
    }

    private void ScrollViewer2OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_updating)
        {
            return;
        }

        _updating = true;
        ScrollViewer1.Offset = ScrollViewer2.Offset;
        _updating = false;
    }

    protected override void OnUnloaded(RoutedEventArgs routedEventArgs)
    {
        base.OnUnloaded(routedEventArgs);
        
        _lineReader1?.Dispose();
        _lineReader2?.Dispose();
    }
}
