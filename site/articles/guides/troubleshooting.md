---
title: "Troubleshooting"
---

# Troubleshooting

## The control renders blank

Cause:

- `LineReader` or `HexFormatter` was never assigned

Fix:

- assign both properties before first render
- call `InvalidateScrollable()` after opening a file

## Editing highlights work but saving does not

Cause:

- edits are staying in the control's internal `_edits` cache because `ByteWriteAction` is not wired

Fix:

- route writes into `ByteOverlay` through `ByteWriteAction`
- expose changed offsets through `EditedOffsetsProvider`

## Opening a file for write fails after reading

Cause:

- the existing `MemoryMappedLineReader` is still holding the file

Fix:

- dispose the current reader before reopening for write access

## Search seems to miss matches near chunk boundaries

Cause:

- a custom reader implementation may not be honoring the requested `Read` contract

Fix:

- verify `Read(offset, buffer, count)` returns contiguous bytes correctly
- compare behavior against `MemoryMappedLineReader`

## Annotation dragging interferes with selection

Cause:

- dragging is only intended when `Ctrl` is held on an annotated range

Fix:

- plain click or drag should be used for normal selection
- `Ctrl`+drag should be reserved for moving annotations
