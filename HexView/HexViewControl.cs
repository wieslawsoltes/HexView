using System;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

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
    private Typeface _typeface;
    private double _lineHeight;
    private FontFamily _fontFamily;
    private double _fontSize;

    public HexViewState? State { get;  set; }

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

    // TODO: Use LineHeight
    Size ILogicalScrollable.ScrollSize => new Size(1, _lineHeight);

    // TODO: Use LineHeight
    Size ILogicalScrollable.PageScrollSize => new Size(10, _lineHeight);

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

    protected override void OnLoaded()
    {
        base.OnLoaded();
        
        _fontFamily = TextElement.GetFontFamily(this);
        _fontSize =  TextElement.GetFontSize(this);
        
        _typeface = new Typeface(_fontFamily);

        var ft = new FormattedText(
            "0",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            _fontSize,
            Brushes.Black);

        _lineHeight = ft.Height;

        InvalidateScrollable();
    }

    public void InvalidateScrollable()
    {
        if (this is not ILogicalScrollable scrollable)
        {
            return;
        }

        var lines = State?.Lines ?? 0;
        var width = Bounds.Width;
        var height = Bounds.Height;
        var viewport = new Size(width, height);
        var extent = new Size(width, _lineHeight * lines); // Text height * 1000 lines
        //var offset = new Vector(0, 0);

        //Console.WriteLine($"{Bounds.Width} {Bounds.Height} {_offset}");
        _extent = extent;
        //_offset = offset;
        _viewport = viewport;

        scrollable.RaiseScrollInvalidated(EventArgs.Empty);
        
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (State is null)
        {
            return;
        }

        var startLine = (long)Math.Floor((_offset.Y / _lineHeight));
        var lines = _viewport.Height / _lineHeight;
        var endLine = (long)(startLine + Math.Ceiling(lines));

        // Console.WriteLine($"Render {startLine}..{endLine}, {State.Lines}");

        var sb = new StringBuilder();
        for (var i = startLine; i <= endLine; i++)
        {
            var bytes = State.GetLine(i);
            State.AddLine(bytes, i, sb);
            sb.AppendLine();
        }

        var text = sb.ToString();

        var ft = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            _fontSize,
            Brushes.Black);

        var origin = new Point();

        context.DrawText(ft, origin);
    }
}
