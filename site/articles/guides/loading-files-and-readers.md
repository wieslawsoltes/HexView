---
title: "Loading Files and Readers"
---

# Loading Files and Readers

The standard file-open path in this repository is:

1. Dispose the previous reader
2. Create `MemoryMappedLineReader`
3. Wrap it in `ByteOverlay`
4. Wrap the overlay in `ByteOverlayLineReader`
5. Create a new `HexFormatter`
6. Reassign the control properties
7. Call `InvalidateScrollable()`

## Why Recreate the Formatter

`HexFormatter` stores the total length and computes line count from that length. When you open a different file or change logical length through insert/delete workflows, recreating the formatter keeps line metrics honest.

## File Ownership

The samples pass an open `FileStream` into `MemoryMappedLineReader`. That lets the host decide how the stream is opened while still centralizing memory-mapped reads inside the reader implementation.

## Disposal Rule

Always dispose the active line reader before reopening the same file for write access. The sample save flow does this explicitly before writing back overwrite edits.
