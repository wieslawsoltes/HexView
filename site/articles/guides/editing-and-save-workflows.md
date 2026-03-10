---
title: "Editing and Save Workflows"
---

# Editing and Save Workflows

HexView supports two edit styles.

## Overwrite

Overwrite mode is the lighter path:

- logical length stays fixed
- byte edits are tracked in the overlay overwrite map
- save-back can patch only changed offsets

## Insert

Insert mode uses the piece-table path in `ByteOverlay`:

- insertions extend logical length
- deletions remove logical ranges
- replacements rebuild affected ranges through delete plus insert

## Undo and Redo

The sample records edits into `EditJournal` as they occur, then calls:

```csharp
_journal.Undo(_overlay);
_journal.Redo(_overlay);
```

## Save Paths

`SaveService` exposes:

- `SaveAs(ByteOverlay overlay, string path)` for full materialization of the logical file
- `ExportPatch(ByteOverlay overlay)` for a simple text patch listing overwrites, inserts, and deletes

The single-view sample also includes an in-place overwrite save path for the currently opened file.

## Recommendation

If you need an editor:

- route UI writes through `ByteWriteAction`
- track changes with `EditedOffsetsProvider`
- keep save logic outside the control

That preserves the separation between rendering and persistence.
