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
    private volatile bool _updating;
    private Size _extent;
    private Size _viewport;
    private Vector _offset;
    private bool _canHorizontallyScroll;
    private bool _canVerticallyScroll;
    private EventHandler? _scrollInvalidated;
    private Typeface _typeface;
    private double _lineHeight;
    private FontFamily? _fontFamily;
    private double _fontSize;
    private Size _scrollSize = new(1, 1);
    private Size _pageScrollSize = new(10, 10);

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
            _offset = CoerceOffset(value);
            //_offset = value;
            //InvalidateMeasure();
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

    Size ILogicalScrollable.ScrollSize => _scrollSize;

    Size ILogicalScrollable.PageScrollSize => _pageScrollSize;

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
    
    private Vector CoerceOffset(Vector value)
    {
        var scrollable = (ILogicalScrollable)this;
        var maxX = Math.Max(scrollable.Extent.Width - scrollable.Viewport.Width, 0);
        var maxY = Math.Max(scrollable.Extent.Height - scrollable.Viewport.Height, 0);
        return new Vector(Clamp(value.X, 0, maxX), Clamp(value.Y, 0, maxY));
        static double Clamp(double val, double min, double max) => val < min ? min : val > max ? max : val;
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
        var extent = new Size(width, lines * _lineHeight); // Text height * 1000 lines
        //var offset = new Vector(0, 0);

        _scrollSize = new Size(1, _lineHeight);
        _pageScrollSize = new Size(_viewport.Width, _viewport.Height);

        //Console.WriteLine($"{Bounds.Width} {Bounds.Height} {_offset}");
        _extent = extent;
        //_offset = offset;
        _viewport = viewport;

        scrollable.RaiseScrollInvalidated(EventArgs.Empty);
        
        InvalidateVisual();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        finalSize = base.ArrangeOverride(finalSize);
        
        InvalidateScrollable();

        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (State is null)
        {
            return;
        }

        var startLine = (long)Math.Ceiling(_offset.Y / _lineHeight);
        var lines = _viewport.Height / _lineHeight;
        var endLine = (long)Math.Floor(startLine + lines);
        endLine = Math.Min(endLine, State.Lines - 1);
        
        //Console.WriteLine($"{startLine}..{endLine}, Lines={State.Lines}, extent={_extent.Height}, offset={_offset.Y}, _viewport={_viewport.Height}");

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
