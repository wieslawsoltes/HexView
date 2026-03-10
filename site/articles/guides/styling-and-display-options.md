---
title: "Styling and Display Options"
---

# Styling and Display Options

HexView uses Avalonia properties for most display customization.

## Control Properties

- `ToBase`
- `BytesWidth`
- `SelectionStart`
- `SelectionLength`
- `Annotations`

## Brush Properties

- `CaretBrush`
- `EditedBrush`
- `SelectionBrush`
- `MatchBrush`
- `DiffBrush`

## Text Properties

The samples style the control with attached `TextElement` properties:

- `TextElement.FontFamily`
- `TextElement.FontSize`
- `TextElement.Foreground`

## Formatter Options

`HexFormatter` controls:

- `GroupSize`
- `ShowGroupSeparator`
- `AddressWidthOverride`
- `Encoding`
- `UseControlGlyph`
- `ControlGlyph`

This split is intentional:

- control properties handle interaction and highlight behavior
- formatter properties handle textual representation
