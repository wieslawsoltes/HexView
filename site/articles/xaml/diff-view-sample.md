---
title: "Diff View Sample"
---

# Diff View Sample

`DiffView` demonstrates a side-by-side comparison host.

## Key Behaviors

- two `HexViewControl` instances share display settings
- each side has its own reader and overlay
- diff offsets are recomputed into a shared sorted set
- scroll positions can be synchronized
- next and previous diff navigation moves both carets together
- optional lock-step edits mirror writes into both overlays

## When To Use It

Use the diff sample as a starting point when you need:

- binary compare tools
- patch review helpers
- synchronized navigation across two files
