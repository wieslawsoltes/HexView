---
title: "API Coverage Index"
---

# API Coverage Index

The Lunet API site is generated from the docs-only API project:

- `site/api-src/HexView.ApiDocs.csproj`

## Main Coverage Areas

- Control surface
  - <xref:HexView.Avalonia.Controls.HexViewControl>
- Reader and formatter abstractions
  - <xref:HexView.Avalonia.Model.ILineReader>
  - <xref:HexView.Avalonia.Model.IHexFormatter>
- File and overlay services
  - <xref:HexView.Avalonia.Services.MemoryMappedLineReader>
  - <xref:HexView.Avalonia.Services.ByteOverlay>
  - <xref:HexView.Avalonia.Services.ByteOverlayLineReader>
  - <xref:HexView.Avalonia.Services.HexFormatter>
- Editing and workflow services
  - <xref:HexView.Avalonia.Services.SelectionService>
  - <xref:HexView.Avalonia.Services.NavigationService>
  - <xref:HexView.Avalonia.Services.EditJournal>
  - <xref:HexView.Avalonia.Services.HexSearchService>
  - <xref:HexView.Avalonia.Services.SaveService>
- Annotation types
  - <xref:HexView.Avalonia.Model.HexAnnotation>
  - <xref:HexView.Avalonia.Services.HexAnnotationStorage>
