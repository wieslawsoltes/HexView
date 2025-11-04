using Avalonia.Markup.Xaml;

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
