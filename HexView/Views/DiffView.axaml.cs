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
    private ByteOverlay? _overlay1;
    private ByteOverlayLineReader? _overlayReader1;
    private ByteOverlay? _overlay2;
    private ByteOverlayLineReader? _overlayReader2;
    private System.Collections.Generic.SortedSet<long> _diffOffsets = new();
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
        _overlay1 = new ByteOverlay(_lineReader1);
        _overlayReader1 = new ByteOverlayLineReader(_overlay1);
        _hexFormatter1 = new HexFormatter(_overlay1.Length);
        HexViewControl1.LineReader = _overlayReader1;
        HexViewControl1.HexFormatter = _hexFormatter1;
        HexViewControl1.ByteWriteAction = (off, val) => { _overlay1!.OverwriteByte(off, val); if (LockStepCheckBox.IsChecked == true && _overlay2 is { }) _overlay2.OverwriteByte(off, val); RecomputeDiffOffsets(); };
        HexViewControl1.InvalidateScrollable();
        // TODO: path
    }

    private void OpenFile2(FileStream stream, string path)
    {
        _lineReader2?.Dispose();
        _lineReader2 = new MemoryMappedLineReader(stream);
        _overlay2 = new ByteOverlay(_lineReader2);
        _overlayReader2 = new ByteOverlayLineReader(_overlay2);
        _hexFormatter2 = new HexFormatter(_overlay2.Length);
        HexViewControl2.LineReader = _overlayReader2;
        HexViewControl2.HexFormatter = _hexFormatter2;
        HexViewControl2.ByteWriteAction = (off, val) => { _overlay2!.OverwriteByte(off, val); if (LockStepCheckBox.IsChecked == true && _overlay1 is { }) _overlay1.OverwriteByte(off, val); RecomputeDiffOffsets(); };
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

    

    private void RecomputeDiffOffsets()
    {
        _diffOffsets.Clear();
        if (_overlayReader1 is null || _overlayReader2 is null) return;
        long len = System.Math.Min(_overlayReader1.Length, _overlayReader2.Length);
        int chunk = 262144;
        var buf1 = new byte[chunk];
        var buf2 = new byte[chunk];
        long pos = 0;
        while (pos < len)
        {
            int toRead = (int)System.Math.Min(chunk, len - pos);
            int r1 = _overlayReader1.Read(pos, buf1, toRead);
            int r2 = _overlayReader2.Read(pos, buf2, toRead);
            int r = System.Math.Min(r1, r2);
            for (int i = 0; i < r; i++)
            {
                if (buf1[i] != buf2[i])
                {
                    _diffOffsets.Add(pos + i);
                }
            }
            pos += r;
            if (r <= 0) break;
        }
        HexViewControl1.DifferencesProvider = (start, end) => _diffOffsets.GetViewBetween(start, end);
        HexViewControl2.DifferencesProvider = (start, end) => _diffOffsets.GetViewBetween(start, end);
        DiffCountTextBlock.Text = _diffOffsets.Count.ToString();
        HexViewControl1.InvalidateVisual();
        HexViewControl2.InvalidateVisual();
    }

    private void PrevDiffButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_diffOffsets.Count == 0) return;
        var off = HexViewControl1.CaretOffset;
        var view = _diffOffsets.GetViewBetween(0, System.Math.Max(0, off - 1));
        long target = view.Count > 0 ? System.Linq.Enumerable.Last(view) : System.Linq.Enumerable.Last(_diffOffsets);
        HexViewControl1.MoveCaretTo(target);
        HexViewControl2.MoveCaretTo(target);
    }

    private void NextDiffButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_diffOffsets.Count == 0) return;
        var off = HexViewControl1.CaretOffset;
        var view = _diffOffsets.GetViewBetween(System.Math.Min(off + 1, long.MaxValue), long.MaxValue);
        long target = view.Count > 0 ? System.Linq.Enumerable.First(view) : System.Linq.Enumerable.First(_diffOffsets);
        HexViewControl1.MoveCaretTo(target);
        HexViewControl2.MoveCaretTo(target);
    }
protected override void OnUnloaded(RoutedEventArgs routedEventArgs)
    {
        base.OnUnloaded(routedEventArgs);
        
        _lineReader1?.Dispose();
        _lineReader2?.Dispose();
    }
}
