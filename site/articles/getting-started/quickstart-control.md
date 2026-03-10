---
title: "Quickstart Control"
---

# Quickstart Control

This path hosts `HexViewControl` directly in your own Avalonia view.

## XAML

```xml
<UserControl
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:hex="clr-namespace:HexView.Avalonia.Controls;assembly=HexView">

  <ScrollViewer HorizontalScrollBarVisibility="Disabled"
                VerticalScrollBarVisibility="Auto">
    <hex:HexViewControl x:Name="HexView"
                        ToBase="16"
                        BytesWidth="16"
                        DragDrop.AllowDrop="True" />
  </ScrollViewer>
</UserControl>
```

## Code-behind

```csharp
using System.Linq;
using HexView.Avalonia.Services;

private MemoryMappedLineReader? _reader;
private ByteOverlay? _overlay;
private ByteOverlayLineReader? _overlayReader;

private void OpenFile(string path)
{
    _reader?.Dispose();

    _reader = new MemoryMappedLineReader(path);
    _overlay = new ByteOverlay(_reader);
    _overlayReader = new ByteOverlayLineReader(_overlay);

    HexView.LineReader = _overlayReader;
    HexView.HexFormatter = new HexFormatter(_overlay.Length)
    {
        GroupSize = 8,
        ShowGroupSeparator = true
    };

    HexView.ByteWriteAction = (offset, value) => _overlay.OverwriteByte(offset, value);
    HexView.EditedOffsetsProvider = (start, end) =>
        _overlay.GetOverwriteEdits().Keys.Where(offset => offset >= start && offset <= end);

    HexView.InvalidateScrollable();
}
```

## Notes

- `HexFormatter` takes a byte length, not a line reader instance.
- `BytesWidth` is a control property; the control pushes that width into the formatter while rendering.
- If you omit `ByteWriteAction`, the control still tracks in-place edits internally, but those edits are not routed through your overlay/save pipeline.

## Next

- Continue with [Quickstart Sample](quickstart-sample.md) for the full repository workflow
- Read [Embedding HexViewControl](../guides/embedding-hexview-control.md) for a fuller host pattern
