---
title: "Diagnostics and Testing"
---

# Diagnostics and Testing

HexView currently ships without a dedicated automated test project, so practical validation usually means:

- building the library and sample apps
- running the sample views against real binary files
- verifying search, edit, save, diff, and annotation paths manually

## Useful Diagnostics Targets

- file reopen and dispose flows
- overwrite versus insert behavior
- chunk-boundary search behavior
- diff recomputation after edits
- annotation load, save, and drag synchronization

## Good Next Steps

If you expand test coverage later, the highest-value targets are:

- `HexSearchService`
- `ByteOverlay`
- `EditJournal`
- `HexAnnotationStorage`
- control-level selection and editing gestures
