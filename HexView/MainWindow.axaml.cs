using System;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;

namespace HexView;

public partial class MainWindow : Window
{
    private HexViewState? _hexViewState;

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects &= (DragDropEffects.Copy | DragDropEffects.Link);

        if (!e.Data.Contains(DataFormats.FileNames))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.FileNames))
        {
            var path = e.Data.GetFileNames()?.FirstOrDefault();
            if (path is { })
            {
                _hexViewState?.Dispose();
                _hexViewState = new HexViewState(path);
                HexViewControl.State = _hexViewState;
                HexViewControl.InvalidateScrollable();
            }
        }
    }

    protected override void OnLoaded()
    {
        base.OnLoaded();

        var path = @"/Users/wieslawsoltes/Documents/GitHub/Acdparser/clippitMS/CLIPPIT.ACS";
        //var path = @"/Users/wieslawsoltes/Downloads/Windows11_InsiderPreview_Client_ARM64_en-us_25158.VHDX";
        //var path = @"c:\Users\Administrator\Documents\GitHub\Acdparser\clippitMS\CLIPPIT.ACS";

        _hexViewState = new HexViewState(path);
        HexViewControl.State = _hexViewState;
        HexViewControl.InvalidateScrollable();
    }

    protected override void OnUnloaded()
    {
        base.OnUnloaded();
        
        _hexViewState?.Dispose();
    }
}
