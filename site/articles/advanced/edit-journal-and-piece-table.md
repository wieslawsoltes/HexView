---
title: "Edit Journal and Piece Table"
---

# Edit Journal and Piece Table

Two internals matter most in editable scenarios.

## Piece Table

`ByteOverlay` uses:

- original pieces that reference the underlying reader
- inserted pieces that reference an append-only insert buffer

That lets insert and delete operations reshape the logical document without mutating the original source immediately.

## Edit Journal

`EditJournal` records logical operations, not UI gestures. This keeps undo and redo independent from the view.

## Batch Operations

Use `BeginBatch()` and `EndBatch()` when one user command should undo as a single unit, such as replace-all or range transforms.
