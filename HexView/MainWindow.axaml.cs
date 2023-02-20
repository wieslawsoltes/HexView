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

        // var path = @"/Users/wieslawsoltes/Documents/GitHub/Acdparser/clippitMS/CLIPPIT.ACS";
        var path = @"c:\Users\Administrator\Documents\GitHub\Acdparser\clippitMS\CLIPPIT.ACS";

        var hexViewState = new HexViewState(path);
/*
        // var startLine = 0;
        // var endLine = 10;
        var startLine = hexViewState.Lines - 10;
        var endLine =  hexViewState.Lines;
        var sb = new StringBuilder();
        for (var i = startLine; i <= endLine; i++)
        {
            var bytes = hexViewState.GetLine(i);
            hexViewState.AddLine(bytes, i, sb);
            sb.AppendLine();
        }
        Console.WriteLine(sb);
*/
        HexViewControl.State = hexViewState;
        HexViewControl.InvalidateScrollable();
    }
}
