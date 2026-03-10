---
title: "Installation"
---

# Installation

## Package

Install the control package into an Avalonia application:

```bash
dotnet add package HexView
```

The current project targets Avalonia `11.3.12`, so keep the host app on a compatible Avalonia line.

## Namespace

Use the control namespace in XAML:

```xml
xmlns:hex="clr-namespace:HexView.Avalonia.Controls;assembly=HexView"
```

## Minimum Wiring

HexView is not a drop-in file loader by itself. You must supply:

- `LineReader`
- `HexFormatter`

For editable scenarios you normally also supply:

- `ByteWriteAction`
- `EditedOffsetsProvider`

## Samples in This Repository

The repository already includes sample hosts:

- `samples/HexView.Base`: shared views and interaction logic
- `samples/HexView.Desktop`: desktop app entry point
- `samples/HexView.Console`: console-flavored sample host

## Docs Tooling

The documentation site uses Lunet, mirroring the TreeDataGrid repository pattern.

From repository root:

```bash
dotnet tool restore
./build-docs.sh
./serve-docs.sh
```

PowerShell:

```powershell
./build-docs.ps1
./serve-docs.ps1
```
