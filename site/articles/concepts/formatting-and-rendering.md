---
title: "Formatting and Rendering"
---

# Formatting and Rendering

`HexViewControl` renders one line at a time based on `ILineReader` and `IHexFormatter`.

## Formatter Responsibilities

`HexFormatter` controls:

- line width
- line count
- offset padding
- byte base: 2, 8, 10, or 16
- group separators
- ASCII encoding and control glyph handling

## Control Responsibilities

`HexViewControl` controls:

- caret movement
- pointer hit testing
- selection rendering
- byte editing gestures
- edited-byte highlighting
- diff highlighting through `DifferencesProvider`
- annotation highlighting through `Annotations`

## Scroll Model

The control implements `ILogicalScrollable`, which lets it cooperate with `ScrollViewer` while keeping rendering virtualized to visible lines.

## Events and Hooks

Important control hooks:

- `CaretMoved`
- `SelectionChanged`
- `AnnotationMoved`
- `ByteWriteAction`
- `EditedOffsetsProvider`
- `DifferencesProvider`

These are the main extension points used by the sample application.
