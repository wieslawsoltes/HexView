using Avalonia.Controls;

namespace HexView;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // var path = @"/Users/wieslawsoltes/Documents/GitHub/Acdparser/clippitMS/CLIPPIT.ACS";
        var path = @"c:\Users\Administrator\Documents\GitHub\Acdparser\clippitMS\CLIPPIT.ACS";

        var hexViewState = new HexViewState(path);

        // var startLine = 0;
        // var endLine = 10;
        var startLine = hexViewState.Lines - 10;
        var endLine =  hexViewState.Lines;
        for (var i = startLine; i <= endLine; i++)
        {
            hexViewState.GetLine(i);
        }

        HexViewControl.State = hexViewState;
        HexViewControl.InvalidateScrollable();
    }
}
