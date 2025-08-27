using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using HexView.Avalonia.Model;
using HexView.Avalonia.Services;

namespace HexView.Views;

public partial class SingleView : UserControl
{
    private ILineReader? _lineReader1;
    private IHexFormatter? _hexFormatter1;
    private string? _currentPath;

    public SingleView()
    {
        InitializeComponent();

        HexViewControl1.AddHandler(DragDrop.DropEvent, Drop);
        HexViewControl1.AddHandler(DragDrop.DragOverEvent, DragOver);
        HexViewControl1.CaretMoved += HexViewControl1OnCaretMoved;
    }

    private void HexViewControl1OnCaretMoved(long offset)
    {
        try
        {
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
        }
        catch
        {
            // ignore if controls not initialized yet
        }
    }

    private void OpenFile(FileStream stream, string path)
    {
        _lineReader1?.Dispose();
        _lineReader1 = new MemoryMappedLineReader(stream);
        _hexFormatter1 = new HexFormatter(stream.Length);
        HexViewControl1.LineReader = _lineReader1;
        HexViewControl1.HexFormatter = _hexFormatter1;
        HexViewControl1.InvalidateScrollable();
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
            var edits = HexViewControl1.GetEdits();
            if (edits.Count == 0)
            {
                return;
            }

            // Release reader to allow writing
            _lineReader1?.Dispose();
            _lineReader1 = null;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                // Write edits in ascending order for more sequential IO
                foreach (var kv in edits.OrderBy(e => e.Key))
                {
                    fs.Position = kv.Key;
                    fs.WriteByte(kv.Value);
                }
                fs.Flush(true);
            }

            // Reopen for reading
            var readStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            _lineReader1 = new MemoryMappedLineReader(readStream);
            HexViewControl1.LineReader = _lineReader1;
            // Keep existing formatter (length unchanged)
            HexViewControl1.ClearEdits();
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

    private static bool TryParseAddress(string? text, out long address)
    {
        address = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return long.TryParse(text.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out address);
        }
        // Try hex if contains A-F
        if (text.IndexOfAny(new[] { 'A','B','C','D','E','F','a','b','c','d','e','f' }) >= 0)
        {
            return long.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out address);
        }
        return long.TryParse(text, out address);
    }

    private static bool TryParseHexBytes(string? text, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim();
        // Remove spaces and 0x markers
        var cleaned = new String(text.Where(c => Uri.IsHexDigit(c)).ToArray());
        if (cleaned.Length < 2 || cleaned.Length % 2 != 0) return false;
        var result = new byte[cleaned.Length / 2];
        for (int i = 0; i < result.Length; i++)
        {
            if (!byte.TryParse(cleaned.AsSpan(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out result[i]))
            {
                return false;
            }
        }
        bytes = result;
        return bytes.Length > 0;
    }

    private long? FindNextValue(byte[] pattern, long startOffset)
    {
        if (HexViewControl1.HexFormatter is null || HexViewControl1.LineReader is null) return null;
        long length = HexViewControl1.HexFormatter.Length;
        if (pattern.Length == 0 || length == 0) return null;

        const int chunkSize = 64 * 1024;
        var buffer = new byte[Math.Max(chunkSize, pattern.Length * 2)];
        long pos = startOffset;
        bool wrapped = false;

        while (true)
        {
            var read = HexViewControl1.LineReader.Read(pos, buffer, buffer.Length);
            if (read <= 0)
            {
                if (wrapped) break;
                pos = 0; wrapped = true; continue;
            }
            var idx = IndexOf(buffer, read, pattern);
            if (idx >= 0)
            {
                return pos + idx;
            }
            pos += Math.Max(1, read - (pattern.Length - 1));
            if (pos >= length)
            {
                if (wrapped) break;
                pos = 0; wrapped = true;
            }
        }
        return null;
    }

    private long? FindPrevValue(byte[] pattern, long startOffset)
    {
        if (HexViewControl1.HexFormatter is null || HexViewControl1.LineReader is null) return null;
        long length = HexViewControl1.HexFormatter.Length;
        if (pattern.Length == 0 || length == 0) return null;

        const int chunkSize = 64 * 1024;
        var buffer = new byte[Math.Max(chunkSize, pattern.Length * 2)];
        long pos = Math.Max(0, startOffset - buffer.Length);
        bool wrapped = false;

        while (true)
        {
            var toRead = (int)Math.Min(buffer.Length, startOffset - pos + 1);
            if (toRead <= 0)
            {
                if (wrapped) break;
                startOffset = length - 1; pos = Math.Max(0, startOffset - buffer.Length); wrapped = true; continue;
            }
            var read = HexViewControl1.LineReader.Read(pos, buffer, toRead);
            if (read <= 0)
            {
                if (wrapped) break;
                startOffset = length - 1; pos = Math.Max(0, startOffset - buffer.Length); wrapped = true; continue;
            }
            var idx = LastIndexOf(buffer, read, pattern);
            if (idx >= 0)
            {
                return pos + idx;
            }
            if (pos == 0)
            {
                if (wrapped) break;
                startOffset = length - 1; pos = Math.Max(0, startOffset - buffer.Length); wrapped = true; continue;
            }
            var step = Math.Max(1, read - (pattern.Length - 1));
            startOffset = pos - 1; // next window end
            pos = Math.Max(0, pos - step);
        }
        return null;
    }

    private static int IndexOf(byte[] buffer, int count, byte[] pattern)
    {
        if (pattern.Length == 0 || count < pattern.Length) return -1;
        int lastStart = count - pattern.Length;
        for (int i = 0; i <= lastStart; i++)
        {
            int j = 0; for (; j < pattern.Length; j++) if (buffer[i + j] != pattern[j]) break; if (j == pattern.Length) return i;
        }
        return -1;
    }

    private static int LastIndexOf(byte[] buffer, int count, byte[] pattern)
    {
        if (pattern.Length == 0 || count < pattern.Length) return -1;
        for (int i = count - pattern.Length; i >= 0; i--)
        {
            int j = 0; for (; j < pattern.Length; j++) if (buffer[i + j] != pattern[j]) break; if (j == pattern.Length) return i;
        }
        return -1;
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
            if (TryParseAddress(query, out var addr))
            {
                var max = Math.Max(0, HexViewControl1.HexFormatter.Length - 1);
                MoveCaretToOffset(Math.Max(0, Math.Min(max, addr)));
            }
            return;
        }

        if (!TryParseHexBytes(query, out var pattern))
        {
            return;
        }

        var start = HexViewControl1.CaretOffset;
        long? found = forward ? FindNextValue(pattern, Math.Min(start + 1, HexViewControl1.HexFormatter.Length - 1))
                              : FindPrevValue(pattern, Math.Max(0, start - 1));
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
}
