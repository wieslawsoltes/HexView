using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;

namespace HexView;

public partial class SingleView : UserControl
{
    private HexViewState? _hexViewState1;

    public SingleView()
    {
        InitializeComponent();

        HexViewControl1.AddHandler(DragDrop.DropEvent, Drop);
        HexViewControl1.AddHandler(DragDrop.DragOverEvent, DragOver);
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
            }
        }
    }

    protected override void OnLoaded()
    {
        base.OnLoaded();
#if DEBUG
        //var path = @"/Users/wieslawsoltes/Documents/GitHub/Acdparser/clippitMS/CLIPPIT.ACS";
        var path = @"c:\Users\Administrator\Documents\GitHub\Acdparser\clippitMS\CLIPPIT.ACS";

        _hexViewState1 = new HexViewState(path);
        HexViewControl1.State = _hexViewState1;
        HexViewControl1.InvalidateScrollable();
#endif
    }

    protected override void OnUnloaded()
    {
        base.OnUnloaded();
        
        _hexViewState1?.Dispose();
    }
}
