using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;

namespace HexView;

public partial class MainWindow : Window
{
    private HexViewState? _hexViewState1;
    private HexViewState? _hexViewState2;

    public MainWindow()
    {
        InitializeComponent();

        HexViewControl1.AddHandler(DragDrop.DropEvent, Drop);
        HexViewControl1.AddHandler(DragDrop.DragOverEvent, DragOver);
 
        HexViewControl2.AddHandler(DragDrop.DropEvent, Drop);
        HexViewControl2.AddHandler(DragDrop.DragOverEvent, DragOver);
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
                if (Equals(sender, HexViewControl1))
                {
                    _hexViewState1?.Dispose();
                    _hexViewState1 = new HexViewState(path);
                    HexViewControl1.State = _hexViewState1;
                    HexViewControl1.InvalidateScrollable();
                }

                if (Equals(sender, HexViewControl2))
                {
                    _hexViewState2?.Dispose();
                    _hexViewState2 = new HexViewState(path);
                    HexViewControl2.State = _hexViewState2;
                    HexViewControl2.InvalidateScrollable();
                }
            }
        }
    }

    protected override void OnLoaded()
    {
        base.OnLoaded();

        //var path = @"/Users/wieslawsoltes/Documents/GitHub/Acdparser/clippitMS/CLIPPIT.ACS";
        //var path = @"/Users/wieslawsoltes/Downloads/Windows11_InsiderPreview_Client_ARM64_en-us_25158.VHDX";
        var path = @"c:\Users\Administrator\Documents\GitHub\Acdparser\clippitMS\CLIPPIT.ACS";

        _hexViewState1 = new HexViewState(path);
        HexViewControl1.State = _hexViewState1;
        HexViewControl1.InvalidateScrollable();

        _hexViewState2 = new HexViewState(path);
        HexViewControl2.State = _hexViewState2;
        HexViewControl2.InvalidateScrollable();
        
        ScrollViewer1.ScrollChanged += ScrollViewer1OnScrollChanged;
        ScrollViewer2.ScrollChanged += ScrollViewer2OnScrollChanged;
    }

    private bool _updating;
    
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

    protected override void OnUnloaded()
    {
        base.OnUnloaded();
        
        _hexViewState1?.Dispose();
        _hexViewState2?.Dispose();
    }
}
