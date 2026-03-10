---
title: "Large Files and Virtualization"
---

# Large Files and Virtualization

HexView is designed around large-file access.

## Why It Scales

- `MemoryMappedLineReader` avoids loading the whole file into memory
- the control renders only visible lines
- search works in chunks instead of on one giant in-memory buffer
- overwrite edits are sparse and keyed by offset

## Practical Notes

- prefer `MemoryMappedLineReader` for real files
- keep expensive transformations out of `Render`
- use `DifferencesProvider` and `EditedOffsetsProvider` to feed only visible ranges

## Insert Mode Caveat

Insert mode is more flexible but also more complex because logical length diverges from physical length. Use overwrite mode when you do not need structural edits.
