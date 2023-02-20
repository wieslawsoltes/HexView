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
    Size ILogicalScrollable.ScrollSize => new Size(1, 14);

    // TODO: Use LineHeight
    Size ILogicalScrollable.PageScrollSize => new Size(10,14);

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

        var lineHeight = 14;
        var lines = State?.Lines ?? 0;

        var width = Bounds.Width;
        var height = Bounds.Height;
        var viewport = new Size(width, height);
        var extent = new Size(width, lineHeight * lines); // Text height * 1000 lines
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

        var lineHeight = 14;

        var startLine = (long)(_offset.Y / lineHeight);
        var endLine = (long)(startLine + _viewport.Height / lineHeight);
        
        //Console.WriteLine($"Render {startLine}..{endLine}");
        
        var sb = new StringBuilder();
        for (var i = startLine; i <= endLine; i++)
        {
            var bytes = State.GetLine(i);
            State.AddLine(bytes, i, sb);
            sb.AppendLine();
        }

        var text = sb.ToString();

        var typeface = new Typeface(TextElement.GetFontFamily(this));

        var ft = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            lineHeight,
            Brushes.Black);

        var origin = new Point();

        context.DrawText(ft, origin);
    }
}
