using System;
using System.Text;
using Avalonia.Controls;

namespace HexView;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded()
    {
        base.OnLoaded();

        //var path = @"/Users/wieslawsoltes/Documents/GitHub/Acdparser/clippitMS/CLIPPIT.ACS";
        var path = @"/Users/wieslawsoltes/Downloads/Windows11_InsiderPreview_Client_ARM64_en-us_25158.VHDX";
        //var path = @"c:\Users\Administrator\Documents\GitHub\Acdparser\clippitMS\CLIPPIT.ACS";

        var hexViewState = new HexViewState(path);

        HexViewControl.State = hexViewState;
        HexViewControl.InvalidateScrollable();
    }
}
