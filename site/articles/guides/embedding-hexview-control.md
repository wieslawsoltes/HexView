---
title: "Embedding HexViewControl"
---

# Embedding HexViewControl

Use this pattern when you want the control without the repository sample UI.

## Recommended Host Structure

- Wrap the control in a `ScrollViewer`
- Keep reader, overlay, and formatter lifetime in the host view or view model
- Dispose file-backed readers on unload or when opening a new file

## Minimal Host State

```csharp
private MemoryMappedLineReader? _reader;
private ByteOverlay? _overlay;
private ByteOverlayLineReader? _overlayReader;
private HexFormatter? _formatter;
```

## When To Add More Services

Add these only when needed:

- `SelectionService` for fill, zero, increment, randomize, and paste helpers
- `NavigationService` for bookmarks and change navigation
- `EditJournal` for undo and redo
- `HexSearchService` for search and replace

## Good Default Settings

- `ToBase = 16`
- `BytesWidth = 8` or `16`
- `GroupSize = 8`
- `ShowGroupSeparator = true`
- `Encoding = ASCII`

Those are also the defaults used throughout the sample surfaces.
