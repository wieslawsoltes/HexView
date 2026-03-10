---
title: "Overlay and Editing Model"
---

# Overlay and Editing Model

`ByteOverlay` is the central edit model.

## Overwrite Mode

In overwrite mode:

- the logical length stays equal to the original reader length
- changed bytes are stored in a sorted overwrite map
- writing the original byte value removes the overwrite entry

This is the cheapest editing mode when you only need in-place changes.

## Insert Mode

In insert mode:

- logical length can diverge from the original file length
- inserts are stored in an append-only insert buffer
- piece metadata tracks how original bytes and inserted bytes are stitched together
- deletes and replace operations update the piece list instead of the original source

## Undo and Redo

`EditJournal` records:

- overwrite operations
- insert operations
- delete operations
- replace operations
- batches

Undo and redo replay those operations against the same `ByteOverlay`.

## Selection and Navigation

Higher-level editing flows are intentionally separate:

- `SelectionService` mutates ranges
- `NavigationService` tracks history, bookmarks, and next-change lookup
- `HexSearchService` finds byte patterns and can drive replace workflows

This makes it easier to reuse the control in read-only viewers or custom editors.
