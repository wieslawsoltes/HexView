---
title: "Custom Readers and Formatters"
---

# Custom Readers and Formatters

HexView is intentionally extensible at two seams.

## Custom Readers

Implement `ILineReader` when your bytes come from something other than a plain file:

- archive members
- remote content
- decompressed views
- test fixtures

The contract is small:

- `GetLine`
- `Read`
- `Length`
- `Dispose`

## Custom Formatters

Implement `IHexFormatter` when you need a different line layout or textual representation.

Examples:

- alternate offset formatting
- domain-specific ASCII panes
- nonstandard grouping

Keep the control contract stable and move display policy into the formatter.
