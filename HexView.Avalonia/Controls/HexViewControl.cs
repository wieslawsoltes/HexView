using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using HexView.Avalonia.Model;

namespace HexView.Avalonia.Controls;

public class HexViewControl : Control, ILogicalScrollable
{
    public static readonly StyledProperty<int> ToBaseProperty = 
        AvaloniaProperty.Register<HexViewControl, int>(nameof(ToBase), defaultValue: 16);

    public static readonly StyledProperty<int> BytesWidthProperty = 
        AvaloniaProperty.Register<HexViewControl, int>(nameof(BytesWidth), defaultValue: 8);

    // Themed brushes
    public static readonly StyledProperty<IBrush> CaretBrushProperty =
        AvaloniaProperty.Register<HexViewControl, IBrush>(nameof(CaretBrush), Brushes.Red);
    public static readonly StyledProperty<IBrush> EditedBrushProperty =
        AvaloniaProperty.Register<HexViewControl, IBrush>(nameof(EditedBrush), Brushes.Orange);
    public static readonly StyledProperty<IBrush> SelectionBrushProperty =
        AvaloniaProperty.Register<HexViewControl, IBrush>(nameof(SelectionBrush), Brushes.LightBlue);
    public static readonly StyledProperty<IBrush> MatchBrushProperty =
        AvaloniaProperty.Register<HexViewControl, IBrush>(nameof(MatchBrush), Brushes.Yellow);
    public static readonly StyledProperty<IBrush> DiffBrushProperty =
        AvaloniaProperty.Register<HexViewControl, IBrush>(nameof(DiffBrush), Brushes.LightGreen);

    public static readonly StyledProperty<long> SelectionStartProperty =
        AvaloniaProperty.Register<HexViewControl, long>(nameof(SelectionStart), 0L);
    public static readonly StyledProperty<long> SelectionLengthProperty =
        AvaloniaProperty.Register<HexViewControl, long>(nameof(SelectionLength), 0L);

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
    private IBrush? _foreground;
    private Size _scrollSize = new(1, 1);
    private Size _pageScrollSize = new(10, 10);
    private double _charWidth;

    // Editing state
    private readonly Dictionary<long, byte> _edits = new();
    private long _caretOffset;
    private int _nibbleIndex; // 0 = high nibble, 1 = low nibble
    private bool _isDragging;
    private bool _dragSelecting;
    private long _selectionAnchor;
    private long _lastClickOffset;
    private long _lastClickTimestampMs;
    public bool IsEditable { get; set; } = true;

    public int ToBase
    {
        get => GetValue(ToBaseProperty);
        set => SetValue(ToBaseProperty, value);
    }

    private bool TrySetCaretFromPoint(Point point)
    {
        if (HexFormatter is null)
        {
            return false;
        }

        var prevOffset = _caretOffset;
        var prevNibble = _nibbleIndex;

        var line = (long)Math.Floor((_offset.Y + point.Y) / _lineHeight);
        line = Math.Max(0, Math.Min(line, HexFormatter.Lines - 1));

        var col = (int)Math.Floor(point.X / Math.Max(1, _charWidth));
        var toBase = ToBase;
        var digitsPerByte = toBase switch { 2 => 8, 8 => 3, 10 => 3, 16 => 2, _ => 2 };
        var prefixLen = (HexFormatter.OffsetPadding) + 2;
        var sepEvery = 8;
        var sepLen = 2;
        var width = HexFormatter.Width;

        if (col < prefixLen)
        {
            return false;
        }

        var sepsFull = (width - 1) / sepEvery;
        var hexAreaLen = sepsFull * sepLen + width * (digitsPerByte + 1);
        var asciiStartCol = prefixLen + hexAreaLen + 3;
        var asciiEndCol = asciiStartCol + width;

        // ASCII hit
        if (col >= asciiStartCol && col < asciiEndCol)
        {
            var jA = Math.Max(0, Math.Min(width - 1, col - asciiStartCol));
            var newOffsetA = line * width + jA;
            if (HexFormatter.Length > 0)
            {
                newOffsetA = Math.Min(newOffsetA, HexFormatter.Length - 1);
            }
            _caretOffset = newOffsetA;
            _nibbleIndex = 0;
            return _caretOffset != prevOffset || _nibbleIndex != prevNibble;
        }

        // Hex hit
        var c = col - prefixLen;
        var found = false;
        for (var j = 0; j < width; j++)
        {
            var sepsBefore = j / sepEvery;
            var startCol = sepsBefore * sepLen + j * (digitsPerByte + 1);
            var endCol = startCol + digitsPerByte;
            if (c >= startCol && c < endCol)
            {
                _caretOffset = line * width + j;
                _nibbleIndex = toBase == 16 && digitsPerByte >= 2 && (c - startCol) >= 1 ? 1 : 0;
                found = true;
                break;
            }
        }

        if (!found)
        {
            for (var j = 0; j < width; j++)
            {
                var sepsBefore = j / sepEvery;
                var startCol = sepsBefore * sepLen + j * (digitsPerByte + 1);
                if (c < startCol)
                {
                    var idx = Math.Max(0, j - 1);
                    _caretOffset = line * width + idx;
                    _nibbleIndex = 0;
                    break;
                }
            }
        }

        return _caretOffset != prevOffset || _nibbleIndex != prevNibble;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging)
        {
            return;
        }

        var pt = e.GetPosition(this);
        if (TrySetCaretFromPoint(pt))
        {
            InvalidateVisual();
            EnsureCaretVisible();
            CaretMoved?.Invoke(_caretOffset);
            if (_dragSelecting)
            {
                UpdateSelectionFromAnchor(_caretOffset);
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            _isDragging = false;
            _dragSelecting = false;
            if (e.Pointer.Captured == this)
            {
                e.Pointer.Capture(null);
            }
            e.Handled = true;
        }
    }
    public int BytesWidth
    {
        get => GetValue(BytesWidthProperty);
        set => SetValue(BytesWidthProperty, value);
    }

    public IHexFormatter? HexFormatter { get;  set; }

    public ILineReader? LineReader { get;  set; }

    public HexViewControl()
    {
        Focusable = true;
        IsTabStop = true;
    }

    public IBrush CaretBrush
    {
        get => GetValue(CaretBrushProperty);
        set => SetValue(CaretBrushProperty, value);
    }
    public IBrush EditedBrush
    {
        get => GetValue(EditedBrushProperty);
        set => SetValue(EditedBrushProperty, value);
    }
    public IBrush SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }
    public IBrush MatchBrush
    {
        get => GetValue(MatchBrushProperty);
        set => SetValue(MatchBrushProperty, value);
    }
    public IBrush DiffBrush
    {
        get => GetValue(DiffBrushProperty);
        set => SetValue(DiffBrushProperty, value);
    }

    public long SelectionStart
    {
        get => GetValue(SelectionStartProperty);
        set => SetValue(SelectionStartProperty, value);
    }

    public long SelectionLength
    {
        get => GetValue(SelectionLengthProperty);
        set => SetValue(SelectionLengthProperty, value);
    }

    public void ClearEdits()
    {
        _edits.Clear();
        InvalidateVisual();
    }

    public IReadOnlyDictionary<long, byte> GetEdits() => _edits;

    public long CaretOffset => _caretOffset;

    public void MoveCaretTo(long offset, int nibbleIndex = 0, bool ensureVisible = true)
    {
        if (HexFormatter is null)
        {
            return;
        }
        var max = Math.Max(0, HexFormatter.Length - 1);
        _caretOffset = Math.Max(0, Math.Min(max, offset));
        _nibbleIndex = nibbleIndex == 0 ? 0 : 1;
        InvalidateVisual();
        if (ensureVisible)
        {
            EnsureCaretVisible();
        }
        CaretMoved?.Invoke(_caretOffset);
    }

    public bool TryGetByteAt(long offset, out byte value)
    {
        value = 0;
        if (HexFormatter is null || LineReader is null) return false;
        if (offset < 0 || offset >= HexFormatter.Length) return false;
        if (_edits.TryGetValue(offset, out var v))
        {
            value = v; return true;
        }
        Span<byte> buf = stackalloc byte[1];
        var tmp = new byte[1];
        var read = LineReader.Read(offset, tmp, 1);
        if (read == 1)
        {
            value = tmp[0];
            return true;
        }
        return false;
    }

    public event Action<long>? CaretMoved;
    public event Action<long, long>? SelectionChanged;

    // Optional hook to forward byte overwrites to an external editing service
    public Action<long, byte>? ByteWriteAction { get; set; }

    // Optional provider for edited offsets to support external editing overlays
    // Signature: (startOffset, endOffset) -> offsets within inclusive range
    public Func<long, long, System.Collections.Generic.IEnumerable<long>>? EditedOffsetsProvider { get; set; }

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
   
    private Vector CoerceOffset(Vector value)
    {
        var scrollable = (ILogicalScrollable)this;
        var maxX = Math.Max(scrollable.Extent.Width - scrollable.Viewport.Width, 0);
        var maxY = Math.Max(scrollable.Extent.Height - scrollable.Viewport.Height, 0);
        return new Vector(Clamp(value.X, 0, maxX), Clamp(value.Y, 0, maxY));
        static double Clamp(double val, double min, double max) => val < min ? min : val > max ? max : val;
    }

    private FormattedText CreateFormattedText(string text)
    {
        return new FormattedText(text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            _fontSize,
            _foreground);
    }

    protected override void OnLoaded(RoutedEventArgs routedEventArgs)
    {
        base.OnLoaded(routedEventArgs);

        Invalidate();
        InvalidateScrollable();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        // Ctrl+wheel zooms font size
        if ((e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            var size = TextElement.GetFontSize(this);
            var delta = e.Delta.Y > 0 ? 1 : -1;
            var newSize = Math.Max(6, size + delta);
            TextElement.SetFontSize(this, newSize);
            Invalidate();
            InvalidateScrollable();
            e.Handled = true;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BoundsProperty)
        {
            InvalidateScrollable();
        }
        
        if (change.Property == TextElement.FontFamilyProperty
            || change.Property == TextElement.FontSizeProperty
            || change.Property == TextElement.ForegroundProperty)
        {
            Invalidate();
            InvalidateScrollable();
        }

        if (change.Property == ToBaseProperty)
        {
            InvalidateVisual();
        }

        if (change.Property == BytesWidthProperty)
        {
            InvalidateScrollable();
        }
    }

    private void Invalidate()
    {
        _fontFamily = TextElement.GetFontFamily(this);
        _fontSize = TextElement.GetFontSize(this);
        _foreground = TextElement.GetForeground(this);
        _typeface = new Typeface(_fontFamily);
        _lineHeight = CreateFormattedText("0").Height;
        _charWidth = CreateFormattedText("0").Width;
    }

    public void InvalidateScrollable()
    {
        if (this is not ILogicalScrollable scrollable)
        {
            return;
        }

        var lines = HexFormatter?.Lines ?? 0;
        var width = Bounds.Width;
        var height = Bounds.Height;

        _scrollSize = new Size(1, _lineHeight);
        _pageScrollSize = new Size(_viewport.Width, _viewport.Height);
        _extent = new Size(width, lines * _lineHeight);
        _viewport = new Size(width, height);

        scrollable.RaiseScrollInvalidated(EventArgs.Empty);
        
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (HexFormatter is null || LineReader is null)
        {
            context.DrawRectangle(Brushes.Transparent, null, Bounds);

            return;
        }
   
        var toBase = ToBase;
        var bytesWidth = BytesWidth;

        if (bytesWidth != HexFormatter.Width)
        {
            HexFormatter.Width = bytesWidth;
        }

        var startLine = (long)Math.Ceiling(_offset.Y / _lineHeight);
        var lines = _viewport.Height / _lineHeight;
        var endLine = (long)Math.Min(Math.Floor(startLine + lines), HexFormatter.Lines - 1);

        var sb = new StringBuilder();
        var lineStartIndices = new List<int>(); // string index of each visible line start
        var digitsPerByte = toBase switch { 2 => 8, 8 => 3, 10 => 3, 16 => 2, _ => 2 };
        var prefixLen = (HexFormatter?.OffsetPadding ?? 0) + 2; // "XXXX: "
        var sepEvery = HexFormatter?.GroupSize ?? 8;
        var sepLen = (HexFormatter?.ShowGroupSeparator ?? true) ? 2 : 0; // "| "

        // Build visible text, applying overlay edits per line
        for (var i = startLine; i <= endLine; i++)
        {
            lineStartIndices.Add(sb.Length);

            var bytes = LineReader.GetLine(i, HexFormatter.Width);

            // Apply overlay edits for this line
            var baseOffset = i * HexFormatter.Width;
            for (var j = 0; j < HexFormatter.Width; j++)
            {
                var pos = baseOffset + j;
                if (_edits.TryGetValue(pos, out var v))
                {
                    bytes[j] = v;
                }
            }

            HexFormatter.AddLine(bytes, i, sb, toBase);
            sb.AppendLine();
        }

        var text = sb.ToString();
        var ft = CreateFormattedText(text);
        var origin = new Point();

        // Draw selection background (hex columns and ASCII columns) before text
        if (HexFormatter is { } selFmt && SelectionLength > 0)
        {
            var selStart = Math.Max(0, SelectionStart);
            var selEnd = Math.Max(selStart, selStart + SelectionLength - 1);
            var selectionBrush = SelectionBrush;

            var sepsFullSel = (selFmt.Width - 1) / sepEvery;
            var hexAreaLenSel = sepsFullSel * sepLen + selFmt.Width * (digitsPerByte + 1);
            var asciiStartColSel = prefixLen + hexAreaLenSel + 3;

            for (var i = startLine; i <= endLine; i++)
            {
                var lineY = (i - startLine) * _lineHeight;
                var baseOffset = i * selFmt.Width;
                for (var j = 0; j < selFmt.Width; j++)
                {
                    var pos = baseOffset + j;
                    if (pos < selStart || pos > selEnd)
                        continue;

                    var sepsB = j / sepEvery;
                    var startCol = prefixLen + sepsB * sepLen + j * (digitsPerByte + 1);
                    var xHex = startCol * _charWidth;
                    var wHex = digitsPerByte * _charWidth;
                    var rectHex = new Rect(xHex, lineY, wHex, _lineHeight);
                    context.DrawRectangle(selectionBrush, null, rectHex);

                    var xAsc = (asciiStartColSel + j) * _charWidth;
                    var rectAsc = new Rect(xAsc, lineY, _charWidth, _lineHeight);
                    context.DrawRectangle(selectionBrush, null, rectAsc);
                }
            }
        }

        var caretBrush = CaretBrush;
        var editedBrush = EditedBrush;

        // Caret highlight in hex area when visible and base 16
        if (toBase == 16 && HexFormatter is { } formatter)
        {
            var caretLine = _caretOffset / formatter.Width;
            if (caretLine >= startLine && caretLine <= endLine)
            {
                var relLine = (int)(caretLine - startLine);
                var byteIndex = (int)(_caretOffset % formatter.Width);

                // Compute per-line constants
                var sepsBefore = byteIndex / sepEvery;
                var startCol = prefixLen + sepsBefore * sepLen + byteIndex * (digitsPerByte + 1);
                var lineGlobalIndex = lineStartIndices[relLine];
                var caretStart = lineGlobalIndex + startCol + (_nibbleIndex == 0 ? 0 : 1);

                // Ensure within this line bounds; highlight one nibble (1 char)
                ft.SetForegroundBrush(caretBrush, caretStart, 1);

                // Also highlight corresponding ASCII character column
                var sepsFull = (formatter.Width - 1) / sepEvery;
                var hexAreaLen = sepsFull * sepLen + formatter.Width * (digitsPerByte + 1);
                var asciiStartCol = prefixLen + hexAreaLen + 3; // " | "
                var asciiCaretStart = lineGlobalIndex + asciiStartCol + byteIndex;
                ft.SetForegroundBrush(caretBrush, asciiCaretStart, 1);
            }
        }

        // Highlight all edited bytes (excluding currently edited nibble/byte in-progress)
        if (HexFormatter is { } fmt)
        {
            var caretByteOffset = _caretOffset;

            var sepsFull = (fmt.Width - 1) / sepEvery;
            var hexAreaLen = sepsFull * sepLen + fmt.Width * (digitsPerByte + 1);
            var asciiStartColAll = prefixLen + hexAreaLen + 3; // " | " before ASCII

            // Build a set of edited offsets for the current visible range
            var visibleStart = startLine * fmt.Width;
            var visibleEnd = endLine * fmt.Width + (fmt.Width - 1);
            var editedSet = new System.Collections.Generic.HashSet<long>();
            if (_edits.Count > 0)
            {
                foreach (var kv in _edits)
                {
                    var pos = kv.Key;
                    if (pos >= visibleStart && pos <= visibleEnd) editedSet.Add(pos);
                }
            }
            if (EditedOffsetsProvider is not null)
            {
                foreach (var pos in EditedOffsetsProvider(visibleStart, visibleEnd))
                {
                    editedSet.Add(pos);
                }
            }

            if (editedSet.Count == 0)
            {
                // nothing to highlight
                goto DrawText;
            }

            for (var i = startLine; i <= endLine; i++)
            {
                var relLine = (int)(i - startLine);
                var lineGlobalIndex = lineStartIndices[relLine];
                var baseOffset = i * fmt.Width;

                for (var j = 0; j < fmt.Width; j++)
                {
                    var pos = baseOffset + j;
                    if (editedSet.Contains(pos))
                    {
                        // Hex column highlight (only if hex mode)
                        if (toBase == 16)
                        {
                            var sepsB = j / sepEvery;
                            var startCol = prefixLen + sepsB * sepLen + j * (digitsPerByte + 1);
                            if (pos == caretByteOffset)
                            {
                                // While caret is on an edited byte, color only the other nibble
                                var otherNibbleCol = startCol + (_nibbleIndex == 0 ? 1 : 0);
                                ft.SetForegroundBrush(editedBrush, lineGlobalIndex + otherNibbleCol, 1);
                            }
                            else
                            {
                                // Color both hex chars for edited byte
                                ft.SetForegroundBrush(editedBrush, lineGlobalIndex + startCol + 0, 1);
                                ft.SetForegroundBrush(editedBrush, lineGlobalIndex + startCol + 1, 1);
                            }
                        }

                        // ASCII column highlight
                        var asciiPos = lineGlobalIndex + asciiStartColAll + j;
                        ft.SetForegroundBrush(editedBrush, asciiPos, 1);
                    }
                }
            }
        }
DrawText:
        context.DrawText(ft, origin);
    }

    private void EnsureCaretVisible()
    {
        if (HexFormatter is null)
        {
            return;
        }

        var startLine = (long)Math.Ceiling(_offset.Y / _lineHeight);
        var lines = _viewport.Height / _lineHeight;
        var endLine = (long)Math.Min(Math.Floor(startLine + lines), HexFormatter.Lines - 1);

        var caretLine = _caretOffset / HexFormatter.Width;
        var scrollable = (ILogicalScrollable)this;

        if (caretLine < startLine)
        {
            var newY = caretLine * _lineHeight;
            scrollable.Offset = new Vector(_offset.X, newY);
        }
        else if (caretLine > endLine)
        {
            var newY = (caretLine + 1) * _lineHeight - _viewport.Height;
            scrollable.Offset = new Vector(_offset.X, newY);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (HexFormatter is null)
        {
            return;
        }

        Focus();

        var point = e.GetPosition(this);
        var line = (long)Math.Floor((_offset.Y + point.Y) / _lineHeight);
        line = Math.Max(0, Math.Min(line, HexFormatter.Lines - 1));

        var col = (int)Math.Floor(point.X / Math.Max(1, _charWidth));
        var toBase = ToBase;
        var digitsPerByte = toBase switch { 2 => 8, 8 => 3, 10 => 3, 16 => 2, _ => 2 };
        var prefixLen = (HexFormatter.OffsetPadding) + 2;
        var sepEvery = 8;
        var sepLen = 2;
        var width = HexFormatter.Width;

        if (col < prefixLen)
        {
            return; // clicked in offset area
        }

        // Precompute ASCII area columns
        var sepsFull = (width - 1) / sepEvery;
        var hexAreaLen = sepsFull * sepLen + width * (digitsPerByte + 1);
        var asciiStartCol = prefixLen + hexAreaLen + 3; // " | "
        var asciiEndCol = asciiStartCol + width; // exclusive

        // Determine byte index from column within hex or ASCII region
        var c = col - prefixLen;
        var found = false;
        // Click in ASCII area → map directly to byte index
        if (col >= asciiStartCol && col < asciiEndCol)
        {
            var j = Math.Max(0, Math.Min(width - 1, col - asciiStartCol));
            var newOffset = line * width + j;
            // Clamp to file length - 1
            if (HexFormatter.Length > 0)
            {
                newOffset = Math.Min(newOffset, HexFormatter.Length - 1);
            }
            _caretOffset = newOffset;
            _nibbleIndex = 0; // start editing with high nibble
            InvalidateVisual();
            EnsureCaretVisible();
            _isDragging = true;
            e.Pointer.Capture(this);
            CaretMoved?.Invoke(_caretOffset);
        // Selection handling (hex area)
        var shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
        if (shift)
        {
            if (!_dragSelecting)
            {
                _selectionAnchor = SelectionLength > 0 ? SelectionStart : _caretOffset;
            }
            _dragSelecting = true;
            // seed selection
            SelectionStart = System.Math.Min(_selectionAnchor, _caretOffset);
            SelectionLength = System.Math.Abs(_caretOffset - _selectionAnchor) + 1;
            SelectionChanged?.Invoke(SelectionStart, SelectionLength);
        }
        else
        {
            _dragSelecting = true;
            _selectionAnchor = _caretOffset;
            SelectionStart = _caretOffset;
            SelectionLength = 1;
            SelectionChanged?.Invoke(SelectionStart, SelectionLength);
        }
            // Selection handling (Shift+Click starts/extends selection; otherwise start drag selection)
            var isShift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
            if (isShift)
            {
                if (!_dragSelecting)
                {
                    _selectionAnchor = SelectionLength > 0 ? SelectionStart : _caretOffset;
                }
                _dragSelecting = true;
                UpdateSelectionFromAnchor(_caretOffset);
            }
            else
            {
                _dragSelecting = true;
                _selectionAnchor = _caretOffset;
                SelectionStart = _caretOffset;
                SelectionLength = 1;
                SelectionChanged?.Invoke(SelectionStart, SelectionLength);
            }
            return;
        }
        for (var j = 0; j < width; j++)
        {
            var sepsBefore = j / sepEvery;
            var startCol = sepsBefore * sepLen + j * (digitsPerByte + 1);
            var endCol = startCol + digitsPerByte; // exclusive of trailing space
            if (c >= startCol && c < endCol)
            {
                _caretOffset = line * width + j;
                // set nibble based on which character within the byte was clicked (only for base 16)
                _nibbleIndex = toBase == 16 && digitsPerByte >= 2 && (c - startCol) >= 1 ? 1 : 0;
                InvalidateVisual();
                EnsureCaretVisible();
                CaretMoved?.Invoke(_caretOffset);
                found = true;
                break;
            }
        }

        if (!found)
        {
            // If clicked on spaces between bytes, snap to closest preceding byte
            for (var j = 0; j < width; j++)
            {
                var sepsBefore = j / sepEvery;
                var startCol = sepsBefore * sepLen + j * (digitsPerByte + 1);
                if (c < startCol)
                {
                    var idx = Math.Max(0, j - 1);
                    _caretOffset = line * width + idx;
                    _nibbleIndex = 0;
                    InvalidateVisual();
                    EnsureCaretVisible();
                    CaretMoved?.Invoke(_caretOffset);
                    break;
                }
            }
        }

        // start drag-to-move caret while pointer pressed
        _isDragging = true;
        e.Pointer.Capture(this);
        CaretMoved?.Invoke(_caretOffset);
    }


    private void UpdateSelectionFromAnchor(long current)
    {
        var start = System.Math.Min(_selectionAnchor, current);
        var end = System.Math.Max(_selectionAnchor, current);
        SelectionStart = start;
        SelectionLength = end - start + 1;
        SelectionChanged?.Invoke(SelectionStart, SelectionLength);
        InvalidateVisual();
    }

    private void SelectAsciiWordAtCaret()
    {
        if (LineReader is null || HexFormatter is null) return;
        var width = HexFormatter.Width;
        var line = _caretOffset / width;
        var lineStart = line * width;
        var idx = (int)(_caretOffset - lineStart);
        var bytes = LineReader.GetLine(line, width);
        bool Printable(byte b) => b >= 0x21 && b <= 0x7E;
        int left = idx;
        while (left > 0 && Printable(bytes[left - 1])) left--;
        int right = idx;
        while (right + 1 < width && Printable(bytes[right + 1])) right++;
        SelectionStart = lineStart + left;
        SelectionLength = right - left + 1;
        _selectionAnchor = SelectionStart;
        _dragSelecting = false;
        SelectionChanged?.Invoke(SelectionStart, SelectionLength);
        InvalidateVisual();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (!IsEditable || HexFormatter is null || LineReader is null)
        {
            return;
        }

        if (ToBase != 16)
        {
            return; // support editing only in hex mode for now
        }

        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        var ch = e.Text![0];
        int nibble;
        if (ch >= '0' && ch <= '9') nibble = ch - '0';
        else if (ch >= 'a' && ch <= 'f') nibble = 10 + (ch - 'a');
        else if (ch >= 'A' && ch <= 'F') nibble = 10 + (ch - 'A');
        else return;

        var width = HexFormatter.Width;
        var line = _caretOffset / width;
        var bytes = LineReader.GetLine(line, width);

        // Apply existing edits on this line
        var baseOffset = line * width;
        for (var j = 0; j < width; j++)
        {
            var pos = baseOffset + j;
            if (_edits.TryGetValue(pos, out var v)) bytes[j] = v;
        }

        var indexInLine = (int)(_caretOffset % width);
        var current = bytes[indexInLine];
        var byteOffset = _caretOffset;
        byte updated;

        if (_nibbleIndex == 0)
        {
            updated = (byte)((nibble << 4) | (current & 0x0F));
            _nibbleIndex = 1; // move to low nibble
        }
        else
        {
            updated = (byte)((current & 0xF0) | nibble);
            _nibbleIndex = 0;
            // advance caret to next byte (if not at end)
            if (byteOffset < HexFormatter.Length - 1)
            {
                _caretOffset = byteOffset + 1;
            }
        }

        if (ByteWriteAction is not null)
        {
            ByteWriteAction(byteOffset, updated);
        }
        else
        {
            _edits[byteOffset] = updated;
        }
        InvalidateVisual();
        EnsureCaretVisible();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (HexFormatter is null)
        {
            return;
        }

        var width = HexFormatter.Width;
        var maxOffset = Math.Max(0, HexFormatter.Length - 1);

        switch (e.Key)
        {
            case Key.Left:
                if (_nibbleIndex == 1)
                {
                    _nibbleIndex = 0;
                }
                else if (_caretOffset > 0)
                {
                    _caretOffset--;
                    _nibbleIndex = 1;
                }
                InvalidateVisual();
                EnsureCaretVisible();
                e.Handled = true;
                break;
            case Key.Right:
                if (_nibbleIndex == 0)
                {
                    _nibbleIndex = 1;
                }
                else if (_caretOffset < maxOffset)
                {
                    _caretOffset++;
                    _nibbleIndex = 0;
                }
                InvalidateVisual();
                EnsureCaretVisible();
                e.Handled = true;
                break;
            case Key.Up:
                if (_caretOffset >= width)
                {
                    _caretOffset -= width;
                }
                InvalidateVisual();
                EnsureCaretVisible();
                e.Handled = true;
                break;
            case Key.Down:
                if (_caretOffset + width <= maxOffset)
                {
                    _caretOffset += width;
                }
                InvalidateVisual();
                EnsureCaretVisible();
                e.Handled = true;
                break;
            case Key.Home:
                _caretOffset -= (_caretOffset % width);
                _nibbleIndex = 0;
                InvalidateVisual();
                EnsureCaretVisible();
                e.Handled = true;
                break;
            case Key.End:
                var endOfLine = Math.Min(maxOffset, _caretOffset - (_caretOffset % width) + width - 1);
                _caretOffset = endOfLine;
                _nibbleIndex = 1;
                InvalidateVisual();
                EnsureCaretVisible();
                e.Handled = true;
                break;
        }

        // Selection with Shift+Arrows: extend selection from anchor
        if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
        {
            if (!_dragSelecting)
            {
                _selectionAnchor = SelectionLength > 0 ? SelectionStart : _caretOffset;
                _dragSelecting = true;
            }
            UpdateSelectionFromAnchor(_caretOffset);
        }
        else
        {
            _dragSelecting = false;
        }
    }
}
