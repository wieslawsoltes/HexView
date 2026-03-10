---
title: "XAML Overview"
---

# XAML Overview

HexView is hosted like a normal Avalonia control. The most common pattern in this repository is:

- a `ScrollViewer`
- one or two `HexViewControl` instances
- toolbar or menu controls that mutate services and forward state back into the control

## Core Binding Style

The samples bind control configuration directly from surrounding controls:

- base selection to `ToBase`
- bytes-per-line selection to `BytesWidth`
- text styling through `TextElement` properties

The control itself stays focused on rendering and interaction, not on application layout.
