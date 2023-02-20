using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace HexView;

public class HexViewControl : Control, ILogicalScrollable
{
    private volatile bool _updating = false;
    private Size _extent;
    private Size _viewport;
    private Vector _offset;
    private bool _canHorizontallyScroll;
    private bool _canVerticallyScroll;
    private EventHandler? _scrollInvalidated;

    Size IScrollable.Extent => _extent;

    Vector IScrollable.Offset
    {
        get => _offset;
        set
        {
            if (_updating)
            {
                return;
            }
            _updating = true;
            _offset = value;
            InvalidateScrollable();
            _updating = false;
        }
    }

    Size IScrollable.Viewport => _viewport;

    bool ILogicalScrollable.CanHorizontallyScroll
    {
        get => _canHorizontallyScroll;
        set
        {
            _canHorizontallyScroll = value;
            InvalidateMeasure();
        }
    }

    bool ILogicalScrollable.CanVerticallyScroll
    {
        get => _canVerticallyScroll;
        set
        {
            _canVerticallyScroll = value;
            InvalidateMeasure();
        }
    }

    bool ILogicalScrollable.IsLogicalScrollEnabled => true;

    event EventHandler? ILogicalScrollable.ScrollInvalidated
    {
        add => _scrollInvalidated += value;
        remove => _scrollInvalidated -= value;
    }

    Size ILogicalScrollable.ScrollSize => new Size(1, 1);

    Size ILogicalScrollable.PageScrollSize => new Size(10, 10);

    bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect)
    {
        return false;
    }

    Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from)
    {
        return null;
    }

    void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e)
    {
        _scrollInvalidated?.Invoke(this, e);
    }

    public void InvalidateScrollable()
    {
        if (this is not ILogicalScrollable scrollable)
        {
            return;
        }

        var width = Bounds.Width;
        var height = Bounds.Height;
        var viewport = new Size(width, height);
        var extent = new Size(width, 14 * 1000); // Text height * 1000 lines
        var offset = new Vector(0, 0);

        Console.WriteLine($"{Bounds.Width} {Bounds.Height} {_offset}");
        _extent = extent;
        //_offset = offset;
        _viewport = viewport;

        scrollable.RaiseScrollInvalidated(EventArgs.Empty);
    }
}

public class HexViewState
{
    private FileInfo _info;
    private MemoryMappedFile _file;
    private MemoryMappedViewAccessor _accessor;
    private int _width;
    private long _lines;

    public HexViewState()
    {
        var path = @"/Users/wieslawsoltes/Documents/GitHub/Acdparser/clippitMS/CLIPPIT.ACS";

        _info = new FileInfo(path); 
        _file = MemoryMappedFile.CreateFromFile(path); 
        _accessor = _file.CreateViewAccessor(0, _info.Length);

        _width = 16; // 8m 16, 24, 32

        _lines = _info.Length / _width;
            
        for (long i = 0; i < _info.Length; i += _width)
        {
            for (var j = 0; j < _width; j++)
            {
                if (j < _info.Length)
                {
                    var value = _accessor.ReadByte(i);
                    Console.Write($"{value:X2}");
                }
            }
            Console.WriteLine();
        }
    }
}

public partial class MainWindow : Window
{
    private readonly HexViewState _hexViewState;

    public MainWindow()
    {
        InitializeComponent();

        HexViewControl.InvalidateScrollable();
        //_hexViewState = new HexViewState();
    }
}
