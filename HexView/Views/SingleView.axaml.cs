// HexView control for Avalonia.
// Copyright (C) 2025  Wiesław Šoltés

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using HexView.Avalonia.Model;
using HexView.Avalonia.Services;
using System.Linq;

namespace HexView.Views;

public partial class SingleView : UserControl
{
    private ILineReader? _lineReader1;
    private IHexFormatter? _hexFormatter1;
    private string? _currentPath;
    private ByteOverlay? _overlay1;
    private ByteOverlayLineReader? _overlayReader1;
    private EditJournal? _journal1;
    private SelectionService? _selection1;
    private NavigationService? _navigation1;

    public SingleView()
    {
        InitializeComponent();

        HexViewControl1.AddHandler(DragDrop.DropEvent, Drop);
        HexViewControl1.AddHandler(DragDrop.DragOverEvent, DragOver);
        HexViewControl1.CaretMoved += HexViewControl1OnCaretMoved;
        HexViewControl1.SelectionChanged += HexViewControl1OnSelectionChanged;
        HexViewControl1.KeyDown += HexViewControl1OnKeyDown;
    }

    private void HexViewControl1OnCaretMoved(long offset)
    {
        try
        {
            _navigation1?.Visit(offset);
            var mode = (SearchModeComboBox.SelectedItem as string) ?? "Address";
            if (string.Equals(mode, "Address", StringComparison.OrdinalIgnoreCase))
            {
                SearchTextBox.Text = $"0x{offset:X}";
            }
            else
            {
                if (HexViewControl1.TryGetByteAt(offset, out var b))
                {
                    SearchTextBox.Text = b.ToString("X2");
                }
            }

            // Update status bar
            StatusOffset.Text = $"0x{offset:X}";
            if (HexViewControl1.TryGetByteAt(offset, out var bb))
            {
                StatusByte.Text = $"{bb:X2}/d{bb}/b{Convert.ToString(bb, 2).PadLeft(8, '0')}";
                var c = (char)bb;
                StatusAscii.Text = char.IsControl(c) ? string.Empty : c.ToString();
            }
            StatusLength.Text = _overlay1?.Length.ToString() ?? "0";
            StatusSel.Text = _selection1?.Length.ToString() ?? "0";

            // If selection start text box is focused, fill with caret offset (decimal)
            if (SelStartTextBox?.IsFocused == true)
            {
                SelStartTextBox.Text = offset.ToString();
                // Keep current selection length; just move the start preview
                HexViewControl1.SelectionStart = offset;
                HexViewControl1.InvalidateVisual();
            }

            // If selection length text box is focused, compute length from start to caret
            if (SelLenTextBox?.IsFocused == true)
            {
                long startVal = 0;
                long.TryParse(SelStartTextBox?.Text, out startVal);
                long len = offset >= startVal ? (offset - startVal + 1) : 0;
                SelLenTextBox.Text = len.ToString();
                HexViewControl1.SelectionStart = startVal;
                HexViewControl1.SelectionLength = len;
                _selection1?.Set(startVal, len);
                StatusSel.Text = len.ToString();
                HexViewControl1.InvalidateVisual();
            }
        }
        catch
        {
            // ignore if controls not initialized yet
        }
    }

    private void EditModeComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_overlay1 is null) return;
        var mode = (EditModeComboBox.SelectedItem as string) ?? "Overwrite";
        _overlay1.Mode = string.Equals(mode, "Insert", StringComparison.OrdinalIgnoreCase)
            ? EditMode.Insert
            : EditMode.Overwrite;
        UpdateDeleteButtonEnabled();
    }

    private void OpenFile(FileStream stream, string path)
    {
        _lineReader1?.Dispose();
        _lineReader1 = new MemoryMappedLineReader(stream);
        _overlay1 = new ByteOverlay(_lineReader1);
        _overlayReader1 = new ByteOverlayLineReader(_overlay1);
        _journal1 = new EditJournal();
        _selection1 = new SelectionService(_overlay1);
        _navigation1 = new NavigationService(_overlay1);
        _hexFormatter1 = new HexFormatter(_overlay1.Length);
        HexViewControl1.LineReader = _overlayReader1;
        HexViewControl1.HexFormatter = _hexFormatter1;
        HexViewControl1.ByteWriteAction = OverwriteByteFromEditor;
        HexViewControl1.EditedOffsetsProvider = (start, end) => _overlay1!.GetOverwriteEdits().Keys.Where(k => k >= start && k <= end);
        HexViewControl1.InvalidateScrollable();
        UpdateUndoRedoButtons();
        UpdateUndoRedoButtons();
        UpdateDeleteButtonEnabled();
        UpdateUndoRedoButtons();
        PathTextBox.Text = path;
        _currentPath = path;
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
                    OpenFile(stream, path);
                }
            }
        }
    }

    protected override void OnLoaded(RoutedEventArgs routedEventArgs)
    {
        base.OnLoaded(routedEventArgs);
#if DEBUG
        //var path = @"/Users/wieslawsoltes/Documents/GitHub/Acdparser/clippitMS/CLIPPIT.ACS";
        //var path = @"c:\Users\Administrator\Documents\GitHub\Acdparser\clippitMS\CLIPPIT.ACS";

        //var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        //OpenFile(stream, path);
#endif
    }

    protected override void OnUnloaded(RoutedEventArgs routedEventArgs)
    {
        base.OnUnloaded(routedEventArgs);
        
        _lineReader1?.Dispose();
    }

    private IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return window.StorageProvider;
        }

        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime { MainView: { } mainView })
        {
            var visualRoot = mainView.GetVisualRoot();
            if (visualRoot is TopLevel topLevel)
            {
                return topLevel.StorageProvider;
            }
        }

        return null;
    }

    private async Task Open()
    {
        var storageProvider = GetStorageProvider();
        if (storageProvider is null)
        {
            return;
        }

        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open",
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("All")
                {
                    Patterns = new[] { "*.*" },
                    MimeTypes = new[] { "*/*" }
                }
            },
            AllowMultiple = false
        });

        var file = result.FirstOrDefault();
        if (file is not null)
        {
            try
            {
                var fileStream = File.OpenRead(file.Path.LocalPath);
                if (file.Path.IsAbsoluteUri)
                {
                    OpenFile(fileStream, file.Path.AbsolutePath);
                }
                else
                {
                    OpenFile(fileStream, file.Path.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    private async void OpenButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await Open();
    }

    private void SaveEditsToFile(string path)
    {
        try
        {
            if (_overlay1 is null)
            {
                return;
            }

            // Release reader to allow writing
            _lineReader1?.Dispose();
            _lineReader1 = null;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                // Overwrite mode writes only modified bytes
                foreach (var kv in _overlay1.GetOverwriteEdits().OrderBy(e => e.Key))
                {
                    fs.Position = kv.Key;
                    fs.WriteByte(kv.Value);
                }
                fs.Flush(true);
            }

            // Reopen for reading
            var readStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            _lineReader1 = new MemoryMappedLineReader(readStream);
            _overlay1 = new ByteOverlay(_lineReader1);
            _overlayReader1 = new ByteOverlayLineReader(_overlay1);
            HexViewControl1.LineReader = _overlayReader1;
            HexViewControl1.ByteWriteAction = OverwriteByteFromEditor;
            _journal1 = new EditJournal();
            _selection1 = new SelectionService(_overlay1);
            _navigation1 = new NavigationService(_overlay1);
            _hexFormatter1 = new HexFormatter(_overlay1.Length);
            HexViewControl1.HexFormatter = _hexFormatter1;
            HexViewControl1.EditedOffsetsProvider = (start, end) => _overlay1!.GetOverwriteEdits().Keys.Where(k => k >= start && k <= end);
            HexViewControl1.InvalidateScrollable();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_currentPath))
        {
            SaveEditsToFile(_currentPath!);
        }
    }

    private void OverwriteByteFromEditor(long offset, byte value)
    {
        if (_overlay1 is null || _journal1 is null)
            return;
        // Capture old value
        var buf = new byte[1];
        _overlay1.Read(offset, buf, 1);
        var old = buf[0];
        if (old == value) return;
        _overlay1.OverwriteByte(offset, value);
        _journal1.Record(new EditOperation
        {
            Type = EditOpType.Overwrite,
            Offset = offset,
            OldData = new[] { old },
            NewData = new[] { value }
        });
        UpdateUndoRedoButtons();
    }

    private void UndoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_overlay1 is null || _journal1 is null) return;
        _journal1.Undo(_overlay1);
        HexViewControl1.InvalidateVisual();
        UpdateUndoRedoButtons();
    }

    private void RedoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_overlay1 is null || _journal1 is null) return;
        _journal1.Redo(_overlay1);
        HexViewControl1.InvalidateVisual();
        UpdateUndoRedoButtons();
    }

    private void SelApplyButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_selection1 is null) return;
        if (long.TryParse(SelStartTextBox.Text, out var s) && long.TryParse(SelLenTextBox.Text, out var l))
        {
            _selection1.Set(s, l);
            HexViewControl1.SelectionStart = s;
            HexViewControl1.SelectionLength = l;
            HexViewControl1.InvalidateVisual();
        }
    }

    private void SelClearButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _selection1?.Clear();
        HexViewControl1.SelectionLength = 0;
        HexViewControl1.InvalidateVisual();
        UpdateUndoRedoButtons();
    }

    private void FillZeroButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_overlay1 is null || _selection1 is null || _journal1 is null) return;
        var start = _selection1.Start;
        var length = _selection1.Length;
        if (length <= 0) return;
        var oldData = ReadOverlayBytes(start, length);
        _selection1.Zero();
        var newData = ReadOverlayBytes(start, length);
        _journal1.Record(new EditOperation
        {
            Type = EditOpType.Replace,
            Offset = start,
            OldData = oldData,
            NewData = newData
        });
        UpdateUndoRedoButtons();
        UpdateUndoRedoButtons();
        HexViewControl1.InvalidateVisual();
        StatusSel.Text = _selection1.Length.ToString();
    }

    private void IncrementButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_overlay1 is null || _selection1 is null || _journal1 is null) return;
        var start = _selection1.Start;
        var length = _selection1.Length;
        if (length <= 0) return;
        var oldData = ReadOverlayBytes(start, length);
        _selection1.Increment();
        var newData = ReadOverlayBytes(start, length);
        _journal1.Record(new EditOperation
        {
            Type = EditOpType.Replace,
            Offset = start,
            OldData = oldData,
            NewData = newData
        });
        HexViewControl1.InvalidateVisual();
        UpdateUndoRedoButtons();
    }

    private void RandomizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_overlay1 is null || _selection1 is null || _journal1 is null) return;
        var start = _selection1.Start;
        var length = _selection1.Length;
        if (length <= 0) return;
        var oldData = ReadOverlayBytes(start, length);
        _selection1.Randomize();
        var newData = ReadOverlayBytes(start, length);
        _journal1.Record(new EditOperation
        {
            Type = EditOpType.Replace,
            Offset = start,
            OldData = oldData,
            NewData = newData
        });
        HexViewControl1.InvalidateVisual();
    }

    private async void CopyHexButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string text = string.Empty;
        if (_selection1 is not null && _selection1.Length > 0)
        {
            text = _selection1.CopyHex();
        }
        else
        {
            if (HexViewControl1.TryGetByteAt(HexViewControl1.CaretOffset, out var b))
            {
                text = b.ToString("X2");
            }
        }
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    private async void CopyAsciiButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string text = string.Empty;
        if (_selection1 is not null && _selection1.Length > 0)
        {
            text = _selection1.CopyAscii();
        }
        else
        {
            if (HexViewControl1.TryGetByteAt(HexViewControl1.CaretOffset, out var b))
            {
                var c = (char)b;
                text = char.IsControl(c) ? string.Empty : c.ToString();
            }
        }
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    private async void PasteHexButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_selection1 is null || _journal1 is null) return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        var text = clipboard is not null ? await clipboard.GetTextAsync() : null;
        if (!string.IsNullOrEmpty(text))
        {
            var insert = string.Equals((EditModeComboBox.SelectedItem as string), "Insert", StringComparison.OrdinalIgnoreCase);
            var start = _selection1.Start;
            var length = _selection1.Length;
            if (insert)
            {
                if (HexSearchService.TryParseHexBytes(text, out var newData))
                {
                    _selection1.PasteHex(text!, insert);
                    _journal1.Record(new EditOperation
                    {
                        Type = EditOpType.Insert,
                        Offset = start,
                        OldData = Array.Empty<byte>(),
                        NewData = newData
                    });
                }
            }
            else
            {
                if (HexSearchService.TryParseHexBytes(text, out var newData))
                {
                    var oldLen = System.Math.Max(length, (long)newData.Length);
                    var oldData = ReadOverlayBytes(start, oldLen);
                    _selection1.PasteHex(text!, insert);
                    _journal1.Record(new EditOperation
                    {
                        Type = EditOpType.Replace,
                        Offset = start,
                        OldData = oldData,
                        NewData = newData
                    });
                    UpdateUndoRedoButtons();
                }
            }
            HexViewControl1.InvalidateVisual();
        }
    }

    private async void PasteAsciiButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_selection1 is null || _journal1 is null) return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        var text = clipboard is not null ? await clipboard.GetTextAsync() : null;
        if (!string.IsNullOrEmpty(text))
        {
            var insert = string.Equals((EditModeComboBox.SelectedItem as string), "Insert", StringComparison.OrdinalIgnoreCase);
            var start = _selection1.Start;
            var length = _selection1.Length;
            var data = System.Text.Encoding.ASCII.GetBytes(text!);
            if (insert)
            {
                _selection1.PasteAscii(text!, null, insert);
                _journal1.Record(new EditOperation
                {
                    Type = EditOpType.Insert,
                    Offset = start,
                    OldData = Array.Empty<byte>(),
                    NewData = data
                });
            }
            else
            {
                var oldLen = System.Math.Max(length, (long)data.Length);
                var oldData = ReadOverlayBytes(start, oldLen);
                _selection1.PasteAscii(text!, null, insert);
                _journal1.Record(new EditOperation
                {
                    Type = EditOpType.Replace,
                    Offset = start,
                    OldData = oldData,
                    NewData = data
                });
                UpdateUndoRedoButtons();
            }
            HexViewControl1.InvalidateVisual();
        }
    }

    private void ReplaceNextButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_overlay1 is null || HexViewControl1.LineReader is null || _journal1 is null) return;
        if (!HexSearchService.TryParseHexBytes(SearchTextBox.Text, out var find)) return;
        if (!HexSearchService.TryParseHexBytes(ReplaceTextBox.Text, out var repl)) return;
        var start = HexViewControl1.CaretOffset;
        var length = HexViewControl1.HexFormatter!.Length;
        var found = HexSearchService.FindNextValue(HexViewControl1.LineReader, length, find, System.Math.Min(start + 1, length - 1));
        if (found.HasValue)
        {
            var at = found.Value;
            var oldData = ReadOverlayBytes(at, find.Length);
            _overlay1.ReplaceRange(at, find.Length, repl);
            _journal1.Record(new EditOperation
            {
                Type = EditOpType.Replace,
                Offset = at,
                OldData = oldData,
                NewData = repl
            });
            HexViewControl1.MoveCaretTo(at);
            UpdateUndoRedoButtons();
            HexViewControl1.InvalidateVisual();
        }
    }

    private void ReplaceAllButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_overlay1 is null || HexViewControl1.LineReader is null || _journal1 is null) return;
        if (!HexSearchService.TryParseHexBytes(SearchTextBox.Text, out var find)) return;
        if (!HexSearchService.TryParseHexBytes(ReplaceTextBox.Text, out var repl)) return;
        var length = HexViewControl1.HexFormatter!.Length;
        long pos = 0;
        _journal1.BeginBatch();
        while (true)
        {
            var found = HexSearchService.FindNextValue(HexViewControl1.LineReader, length, find, pos);
            if (!found.HasValue) break;
            var at = found.Value;
            var oldData = ReadOverlayBytes(at, find.Length);
            _overlay1.ReplaceRange(at, find.Length, repl);
            _journal1.Record(new EditOperation
            {
                Type = EditOpType.Replace,
                Offset = at,
                OldData = oldData,
                NewData = repl
            });
            pos = at + repl.Length;
            if (pos >= length) break;
        }
        _journal1.EndBatch();
        UpdateUndoRedoButtons();
        HexViewControl1.InvalidateVisual();
    }

    private byte[] ReadOverlayBytes(long offset, long length)
    {
        if (_overlay1 is null || length <= 0) return Array.Empty<byte>();
        var buf = new byte[length];
        _overlay1.Read(offset, buf, buf.Length);
        return buf;
    }

    private void BackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_navigation1 is null) return;
        var pos = _navigation1.Back();
        HexViewControl1.MoveCaretTo(pos);
    }

    private void ForwardButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_navigation1 is null) return;
        var pos = _navigation1.Forward();
        HexViewControl1.MoveCaretTo(pos);
    }

    private void BookmarkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _navigation1?.AddBookmark(HexViewControl1.CaretOffset);
    }

    private void NextChangeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_navigation1 is null) return;
        var next = _navigation1.NextChange(HexViewControl1.CaretOffset);
        if (next >= 0) HexViewControl1.MoveCaretTo(next);
    }

    private async void SaveAsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_overlay1 is null) return;
        var storageProvider = GetStorageProvider();
        if (storageProvider is null) return;
        var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save As",
            SuggestedFileName = "output.bin"
        });
        if (result is not null)
        {
            var path = result.Path.IsAbsoluteUri ? result.Path.AbsolutePath : result.Path.ToString();
            SaveService.SaveAs(_overlay1, path);
        }
    }

    private async void ExportPatchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_overlay1 is null) return;
        var patch = SaveService.ExportPatch(_overlay1);
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(patch);
        }
    }


    private void MoveCaretToOffset(long offset)
    {
        HexViewControl1.MoveCaretTo(offset, 0, ensureVisible: true);
    }

    private void FindCore(bool forward)
    {
        if (HexViewControl1.HexFormatter is null || HexViewControl1.LineReader is null)
        {
            return;
        }

        var mode = (SearchModeComboBox.SelectedItem as string) ?? "Address";
        var query = SearchTextBox.Text;

        if (string.Equals(mode, "Address", StringComparison.OrdinalIgnoreCase))
        {
            if (HexSearchService.TryParseAddress(query, out var addr))
            {
                var max = Math.Max(0, HexViewControl1.HexFormatter.Length - 1);
                MoveCaretToOffset(Math.Max(0, Math.Min(max, addr)));
            }
            return;
        }

        if (!HexSearchService.TryParseHexBytes(query, out var pattern))
        {
            return;
        }

        var start = HexViewControl1.CaretOffset;
        long? found = forward
            ? HexSearchService.FindNextValue(HexViewControl1.LineReader!, HexViewControl1.HexFormatter.Length, pattern, Math.Min(start + 1, HexViewControl1.HexFormatter.Length - 1))
            : HexSearchService.FindPrevValue(HexViewControl1.LineReader!, HexViewControl1.HexFormatter.Length, pattern, Math.Max(0, start - 1));
        if (found.HasValue)
        {
            MoveCaretToOffset(found.Value);
        }
    }

    private void FindNextButton_OnClick(object? sender, RoutedEventArgs e)
    {
        FindCore(true);
    }

    private void FindPrevButton_OnClick(object? sender, RoutedEventArgs e)
    {
        FindCore(false);
    }

    private void GroupSizeComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (HexViewControl1 is null || HexViewControl1.HexFormatter is null) return;
        if (GroupSizeComboBox.SelectedItem is int gs)
        {
            HexViewControl1.HexFormatter.GroupSize = gs;
            HexViewControl1.InvalidateVisual();
        }
    }

    private void ShowSepsCheckBox_OnChanged(object? sender, RoutedEventArgs e)
    {
        if (HexViewControl1 is null || HexViewControl1.HexFormatter is null) return;
        HexViewControl1.HexFormatter.ShowGroupSeparator = ShowSepsCheckBox.IsChecked ?? true;
        HexViewControl1.InvalidateVisual();
    }

    private void AddrWidthTextBox_OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (HexViewControl1 is null || HexViewControl1.HexFormatter is null) return;
        if (int.TryParse(AddrWidthTextBox.Text, out var w))
        {
            HexViewControl1.HexFormatter.AddressWidthOverride = w;
            HexViewControl1.InvalidateScrollable();
        }
    }

    private void EncodingComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (HexViewControl1 is null || HexViewControl1.HexFormatter is null) return;
        var encName = (EncodingComboBox.SelectedItem as string) ?? "ASCII";
        switch (encName.ToUpperInvariant())
        {
            case "ASCII": HexViewControl1.HexFormatter.Encoding = System.Text.Encoding.ASCII; break;
            case "UTF-8": HexViewControl1.HexFormatter.Encoding = System.Text.Encoding.UTF8; break;
            case "UTF-16LE": HexViewControl1.HexFormatter.Encoding = System.Text.Encoding.Unicode; break;
            case "UTF-16BE": HexViewControl1.HexFormatter.Encoding = System.Text.Encoding.BigEndianUnicode; break;
        }
        HexViewControl1.InvalidateVisual();
    }

    private void ControlGlyphCheckBox_OnChanged(object? sender, RoutedEventArgs e)
    {
        if (HexViewControl1 is null || HexViewControl1.HexFormatter is null) return;
        HexViewControl1.HexFormatter.UseControlGlyph = ControlGlyphCheckBox.IsChecked ?? true;
        HexViewControl1.InvalidateVisual();
    }


    private void HexViewControl1OnSelectionChanged(long start, long length)
    {
        _selection1?.Set(start, length);
        StatusSel.Text = length.ToString();
        if (SelStartTextBox?.IsFocused != true)
        {
            SelStartTextBox.Text = start.ToString();
        }
        if (SelLenTextBox?.IsFocused != true)
        {
            SelLenTextBox.Text = length.ToString();
        }
    }

    private void DeleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_overlay1 is null || _selection1 is null || _journal1 is null) return;
        if (_overlay1.Mode != EditMode.Insert) return; // delete shifts only in insert mode
        var start = _selection1.Start;
        var length = _selection1.Length;
        if (length <= 0) return;
        var oldData = ReadOverlayBytes(start, length);
        _overlay1.DeleteRange(start, length);
        _journal1.Record(new EditOperation
        {
            Type = EditOpType.Delete,
            Offset = start,
            OldData = oldData,
            NewData = Array.Empty<byte>()
        });
        // Clear selection and refresh formatter (length changed)
        _selection1.Clear();
        HexViewControl1.SelectionLength = 0;
        RefreshFormatterPreserveConfig();
        HexViewControl1.InvalidateScrollable();
    }

    private void RefreshFormatterPreserveConfig()
    {
        if (_overlay1 is null) return;
        var old = HexViewControl1.HexFormatter;
        if (old is null)
        {
            HexViewControl1.HexFormatter = new HexFormatter(_overlay1.Length);
            return;
        }
        var fmt = new HexFormatter(_overlay1.Length)
        {
            Width = old.Width,
            GroupSize = old.GroupSize,
            ShowGroupSeparator = old.ShowGroupSeparator,
            AddressWidthOverride = old.AddressWidthOverride,
            Encoding = old.Encoding,
            UseControlGlyph = old.UseControlGlyph,
            ControlGlyph = old.ControlGlyph
        };
        HexViewControl1.HexFormatter = fmt;
    }


    private void UpdateUndoRedoButtons()
    {
        if (UndoButton is not null && RedoButton is not null)
        {
            var canUndo = _journal1?.CanUndo == true;
            var canRedo = _journal1?.CanRedo == true;
            UndoButton.IsEnabled = canUndo;
            RedoButton.IsEnabled = canRedo;
        }
    }


    private void UpdateDeleteButtonEnabled()
    {
        if (DeleteButton is not null)
        {
            var insert = string.Equals((EditModeComboBox.SelectedItem as string), "Insert", System.StringComparison.OrdinalIgnoreCase);
            DeleteButton.IsEnabled = insert;
        }
    }


    private async void HexViewControl1OnKeyDown(object? sender, KeyEventArgs e)
    {
        var ctrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
        var shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
        var alt = (e.KeyModifiers & KeyModifiers.Alt) != 0;

        if (ctrl)
        {
            if (e.Key == Key.C)
            {
                if (shift)
                    CopyAsciiButton_OnClick(null, null!);
                else
                    CopyHexButton_OnClick(null, null!);
                e.Handled = true;
            }
            else if (e.Key == Key.V)
            {
                if (shift)
                    PasteAsciiButton_OnClick(null, null!);
                else
                    PasteHexButton_OnClick(null, null!);
                e.Handled = true;
            }
            else if (e.Key == Key.Z)
            {
                // Undo
                UndoButton_OnClick(null, null!);
                e.Handled = true;
            }
            else if (e.Key == Key.Y || (e.Key == Key.Z && shift))
            {
                // Redo
                RedoButton_OnClick(null, null!);
                e.Handled = true;
            }
            else if (e.Key == Key.S && !shift)
            {
                // Save
                SaveButton_OnClick(null, null!);
                e.Handled = true;
            }
            else if (e.Key == Key.S && shift)
            {
                // Save As
                await System.Threading.Tasks.Task.Run(() => SaveAsButton_OnClick(null, null!));
                e.Handled = true;
            }
            else if (e.Key == Key.O)
            {
                await Open();
                e.Handled = true;
            }
            else if (e.Key == Key.F)
            {
                // Focus search
                SearchTextBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.H)
            {
                // Focus replace
                ReplaceTextBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.G)
            {
                // Goto: switch to Address mode and focus search
                SearchModeComboBox.SelectedItem = "Address";
                SearchTextBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.B)
            {
                // Bookmark
                BookmarkButton_OnClick(null, null!);
                e.Handled = true;
            }
            else if (e.Key == Key.A)
            {
                // Select All
                if (_overlay1 is { })
                {
                    _selection1?.Set(0, _overlay1.Length);
                    HexViewControl1.SelectionStart = 0;
                    HexViewControl1.SelectionLength = _overlay1.Length;
                    HexViewControl1.InvalidateVisual();
                    StatusSel.Text = _overlay1.Length.ToString();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Add || (e.Key == Key.OemPlus && !shift))
            {
                // Zoom in
                var size = TextElement.GetFontSize(HexViewControl1);
                TextElement.SetFontSize(HexViewControl1, size + 1);
                HexViewControl1.InvalidateScrollable();
                e.Handled = true;
            }
            else if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
            {
                // Zoom out
                var size = TextElement.GetFontSize(HexViewControl1);
                TextElement.SetFontSize(HexViewControl1, System.Math.Max(6, size - 1));
                HexViewControl1.InvalidateScrollable();
                e.Handled = true;
            }
            else if (e.Key == Key.D0)
            {
                // Reset zoom
                TextElement.SetFontSize(HexViewControl1, 12);
                HexViewControl1.InvalidateScrollable();
                e.Handled = true;
            }
        }

        if (alt)
        {
            if (e.Key == Key.Left)
            {
                BackButton_OnClick(null, null!);
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                ForwardButton_OnClick(null, null!);
                e.Handled = true;
            }
        }

        if (e.Key == Key.F3)
        {
            if (shift) FindPrevButton_OnClick(null, null!);
            else FindNextButton_OnClick(null, null!);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            // Delete selection if in insert mode
            if (DeleteButton.IsEnabled)
            {
                DeleteButton_OnClick(null, null!);
                e.Handled = true;
            }
        }
    }

}
