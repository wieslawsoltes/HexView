---
title: "Annotations and Diff View"
---

# Annotations and Diff View

HexView now supports lightweight annotations and side-by-side diff highlighting.

## Annotations

The control exposes:

- `Annotations`
- `AnnotationMoved`

Annotations are rendered as highlighted byte ranges with optional labels.

Use `HexAnnotationStorage` to persist them:

```csharp
HexAnnotationStorage.Save(path, annotations);
var loaded = HexAnnotationStorage.Load(path);
```

The sample viewer adds:

- add-from-selection actions
- next and previous annotation navigation
- a side panel listing annotations
- `Ctrl`+drag repositioning inside the control

## Diff View

The diff sample computes a sorted set of changed offsets, then assigns:

```csharp
HexViewControl1.DifferencesProvider = (start, end) => _diffOffsets.GetViewBetween(start, end);
HexViewControl2.DifferencesProvider = (start, end) => _diffOffsets.GetViewBetween(start, end);
```

That lets both controls highlight the same changed bytes while keeping their own overlay state.

## Lock-step Editing

The diff sample also shows a higher-level behavior: edits in one view can be mirrored into the other overlay before recomputing diff offsets.
