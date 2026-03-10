---
title: "Quickstart Sample"
---

# Quickstart Sample

The sample app builds a higher-level editor surface around `HexViewControl`.

## Sample Composition

The single-file sample wires these pieces together:

- `MemoryMappedLineReader` for raw file access
- `ByteOverlay` and `ByteOverlayLineReader` for edit projection
- `HexFormatter` for layout and text formatting
- `SelectionService` for range operations
- `NavigationService` for history, bookmarks, and next-change navigation
- `EditJournal` for undo and redo
- `HexSearchService` for address, byte pattern, wildcard, and text search
- `SaveService` for save-as and patch export

## Core Pattern

```csharp
private void OpenFile(FileStream stream, string path)
{
    _lineReader1?.Dispose();
    _lineReader1 = new MemoryMappedLineReader(stream);
    _overlay1 = new ByteOverlay(_lineReader1);
    _overlayReader1 = new ByteOverlayLineReader(_overlay1);
    _journal1 = new EditJournal();
    _selection1 = new SelectionService(_overlay1);
    _navigation1 = new NavigationService(_overlay1);
    _hexFormatter1 = new HexFormatter(_overlay1.Length);

    HexViewControl1.LineReader = _overlayReader1;
    HexViewControl1.HexFormatter = _hexFormatter1;
    HexViewControl1.ByteWriteAction = OverwriteByteFromEditor;
    HexViewControl1.EditedOffsetsProvider =
        (start, end) => _overlay1.GetOverwriteEdits().Keys.Where(k => k >= start && k <= end);
    HexViewControl1.InvalidateScrollable();
}
```

## Why This Matters

This composition keeps responsibilities separate:

- The control owns interaction and rendering
- The services own mutation, history, and navigation
- The sample view owns workflow, dialogs, and status updates

## Next

- Read [Architecture and Data Flow](../concepts/architecture-and-data-flow.md)
- Use [Editing and Save Workflows](../guides/editing-and-save-workflows.md) for the mutation path
