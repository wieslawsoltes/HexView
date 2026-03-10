---
title: "Search, Navigation, and Selection"
---

# Search, Navigation, and Selection

HexView splits these workflows across three services.

## Search

`HexSearchService` supports:

- address parsing
- raw hex byte search
- wildcard search
- text pattern conversion
- replace-next and replace-all helpers

Typical flow:

```csharp
if (HexSearchService.TryParseHexBytes("DE AD BE EF", out var pattern))
{
    var found = HexSearchService.FindNextValue(reader, length, pattern, startOffset);
    if (found.HasValue)
    {
        HexView.MoveCaretTo(found.Value);
    }
}
```

## Navigation

`NavigationService` tracks:

- back stack
- forward stack
- bookmarks
- next changed byte

This keeps caret history independent from the control.

## Selection

`SelectionService` owns the current logical range and exposes operations like:

- `Fill`
- `Zero`
- `Increment`
- `Randomize`
- `CopyHex`
- `CopyAscii`
- `PasteHex`
- `PasteAscii`

The sample keeps the control selection and the service selection synchronized through the `SelectionChanged` event.
