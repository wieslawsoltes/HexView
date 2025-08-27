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
}
