---
title: "Architecture and Data Flow"
---

# Architecture and Data Flow

HexView is organized as a small set of layers.

## Layers

1. Reader layer
   - `ILineReader`
   - `MemoryMappedLineReader`
   - `ByteOverlayLineReader`
2. Edit layer
   - `ByteOverlay`
   - `EditJournal`
   - `SelectionService`
   - `NavigationService`
3. Presentation layer
   - `HexFormatter`
   - `HexViewControl`
4. Workflow layer
   - sample views such as `SingleView` and `DiffView`

## Data Flow

The normal read path is:

```text
file -> MemoryMappedLineReader -> ByteOverlay -> ByteOverlayLineReader -> HexViewControl
```

The edit path is:

```text
keyboard/paste/search-replace -> ByteWriteAction or services -> ByteOverlay -> render refresh
```

## Why The Split Exists

- The control does not have to know how bytes are persisted
- Large files remain practical because reading is chunked and memory-mapped
- Alternate readers and formatters can be introduced without rewriting the control

## Diff and Annotation Paths

- Diff mode uses `DifferencesProvider` to supply changed offsets
- Annotation mode uses `Annotations` plus `HexAnnotationStorage` to draw, navigate, and persist labeled ranges
