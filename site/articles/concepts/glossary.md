---
title: "Glossary"
---

# Glossary

- `ILineReader`: abstraction for reading bytes by offset and by line
- `MemoryMappedLineReader`: file-backed `ILineReader` implementation for large binaries
- `ByteOverlay`: edit projection over an underlying reader
- `ByteOverlayLineReader`: `ILineReader` that exposes overlay state to the control
- `HexFormatter`: builds formatted offset, hex, and ASCII lines
- `HexViewControl`: Avalonia control that renders and edits binary data
- `SelectionService`: range-based mutation helper
- `NavigationService`: caret history and bookmark helper
- `EditJournal`: undo/redo log for overlay operations
- `DifferencesProvider`: callback that returns offsets to highlight as diffs
- `HexAnnotation`: labeled byte range with color and drag behavior
