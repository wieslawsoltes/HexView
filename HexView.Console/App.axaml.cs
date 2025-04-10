using Avalonia;
using Avalonia.Markup.Xaml;
using Consolonia;

namespace HexView.Console
{
    public class App : HexView.App
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
