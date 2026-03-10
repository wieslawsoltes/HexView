---
title: "Getting Started with HexView"
---

# Getting Started with HexView

HexView is an Avalonia control plus a sample application for binary inspection and editing.

It combines:

- A UI control, `HexViewControl`, that renders offsets, byte columns, ASCII columns, selections, diffs, and annotations.
- A reader pipeline based on `ILineReader` so large files can be streamed or memory-mapped.
- An overlay/edit pipeline built on `ByteOverlay`, `SelectionService`, `NavigationService`, and `EditJournal`.

## What You Will Build

By the end of Getting Started, you will have:

- A working `HexViewControl` in XAML
- A `MemoryMappedLineReader` feeding a `ByteOverlayLineReader`
- A `HexFormatter` configured for width, encoding, separators, and address padding
- Optional editing, search, bookmark, diff, and annotation workflows

## Learning Path

1. [Installation](installation.md)
2. [Quickstart Control](quickstart-control.md)
3. [Quickstart Sample](quickstart-sample.md)
4. [Architecture and Data Flow](../concepts/architecture-and-data-flow.md)
5. [Overlay and Editing Model](../concepts/overlay-and-editing-model.md)
6. [Formatting and Rendering](../concepts/formatting-and-rendering.md)
7. [Troubleshooting](../guides/troubleshooting.md)

## Read-only vs Editable

Choose your integration style first:

- Read-only viewer: set `LineReader`, `HexFormatter`, `ToBase`, and `BytesWidth`
- Overlay-backed editor: add `ByteOverlay`, `ByteWriteAction`, `EditedOffsetsProvider`, and save/undo services
- Sample-style editor: compose the control with `SelectionService`, `NavigationService`, `HexSearchService`, `SaveService`, and optional annotations

## Key Idea

HexView separates file access, edit state, and rendering:

- `ILineReader` controls how bytes are read
- `ByteOverlay` controls how edits are projected
- `HexFormatter` controls how bytes are represented
- `HexViewControl` turns all of that into an interactive Avalonia surface

## Next

- Start with [Installation](installation.md)
- Jump to [Quickstart Control](quickstart-control.md) for direct hosting
- Use [Quickstart Sample](quickstart-sample.md) if you want the same service composition as the sample app
