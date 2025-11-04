# HexView

[![NuGet](https://img.shields.io/nuget/v/HexView.svg)](https://www.nuget.org/packages/HexView)
[![NuGet](https://img.shields.io/nuget/dt/HexView.svg)](https://www.nuget.org/packages/HexView)
[![CI](https://github.com/wieslawsoltes/HexView/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/HexView/actions/workflows/build.yml)

A powerful and flexible hexadecimal editor control for [Avalonia](https://avaloniaui.net/), supporting both desktop and console applications. HexView provides comprehensive binary file viewing, editing, comparison, and analysis capabilities with a modern, performant architecture.

## Features

### Core Capabilities

| Category | Features |
|----------|----------|
| **View Modes** | Single file view, Side-by-side diff view with synchronized scrolling |
| **Display Formats** | Hexadecimal (base 16), Decimal (base 10), Octal (base 8), Binary (base 2) |
| **Character Encodings** | ASCII, UTF-8, UTF-16LE, UTF-16BE |
| **Edit Modes** | Overwrite mode, Insert mode with full undo/redo support |
| **Large File Support** | Memory-mapped files, Virtual scrolling, Efficient chunked operations |
| **Search & Replace** | Pattern search, Wildcard matching, Text search, Find & replace with batch operations |
| **Navigation** | Address jump, Back/forward history, Bookmarks, Next/previous difference |
| **Selection Operations** | Copy/paste (hex & ASCII), Fill, Zero-fill, Increment, Randomize |
| **Visual Customization** | Configurable colors, Font size adjustment, Customizable bytes per line |
| **File Operations** | Save, Save As, Export patch, Drag & drop support |

### Advanced Features

| Feature | Description |
|---------|-------------|
| **Piece Table Editing** | Efficient in-memory edit tracking without modifying original file |
| **Byte Overlay System** | Virtual overlay for tracking edits with support for insert/delete operations |
| **Edit Journal** | Unlimited undo/redo with batch operation support |
| **Difference Highlighting** | Automatic visual highlighting of differences in diff mode |
| **Extensible Architecture** | Interface-based design for custom formatters and data sources |
| **Keyboard Shortcuts** | Comprehensive keyboard navigation and operation shortcuts |
| **Event System** | Caret moved, Selection changed, and edit notifications |
| **External Integration** | Callbacks for edit tracking and custom diff providers |

## Screenshots

### Desktop Application

![HexView Desktop](https://github.com/user-attachments/assets/07511c4f-812d-4b35-85b0-4170881b15c5)

### Console Application

![HexView Console](https://github.com/user-attachments/assets/1093fa4e-ac7a-465d-b1bf-2debdeb81982)

## Installation

Install the HexView NuGet package:

```bash
dotnet add package HexView
```

Or via Package Manager:

```powershell
Install-Package HexView
```

## Quick Start

### Basic Usage

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:hexView="clr-namespace:HexView.Avalonia.Controls;assembly=HexView">

    <hexView:HexViewControl x:Name="HexView" />

</Window>
```

```csharp
using HexView.Avalonia.Controls;
using HexView.Avalonia.Services;

// Load a file
var lineReader = new MemoryMappedLineReader("path/to/file.bin");
var formatter = new HexFormatter(lineReader);
HexView.LineReader = lineReader;
HexView.HexFormatter = formatter;
```

### Opening Files

```csharp
public async Task OpenFile(string filePath)
{
    // Create line reader for memory-mapped file access
    var lineReader = new MemoryMappedLineReader(filePath);

    // Create formatter with default settings (base 16, 16 bytes per line)
    var formatter = new HexFormatter(lineReader)
    {
        BytesWidth = 16,  // Bytes per line
        Base = 16,        // Hexadecimal
        GroupSize = 8,    // Group separator every 8 bytes
        ShowGroupSeparator = true
    };

    // Create byte overlay for edit tracking
    var overlay = new ByteOverlay(lineReader);
    var overlayReader = new ByteOverlayLineReader(overlay);

    // Assign to control
    HexView.LineReader = overlayReader;
    HexView.HexFormatter = formatter;
}
```

## Usage Examples

### Configuring Display Format

```csharp
// Switch to binary display
formatter.Base = 2;

// Switch to decimal display
formatter.Base = 10;

// Change bytes per line
formatter.BytesWidth = 8;

// Configure ASCII encoding
formatter.Encoding = Encoding.UTF8;
```

### Edit Tracking with ByteOverlay

```csharp
// Create overlay for tracking edits
var baseReader = new MemoryMappedLineReader("file.bin");
var overlay = new ByteOverlay(baseReader);
var overlayReader = new ByteOverlayLineReader(overlay);

// Enable editing
HexView.Editable = true;
HexView.LineReader = overlayReader;

// Programmatically edit bytes
overlay.OverwriteByte(0x100, 0xFF);
overlay.InsertBytes(0x200, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
overlay.DeleteRange(0x300, 4);

// Check if file has been modified
bool hasEdits = overlay.GetOverwriteEdits().Any() ||
                overlay.GetInserts().Any() ||
                overlay.GetDeletions().Any();
```

### Undo/Redo Support

```csharp
// Create edit journal
var journal = new EditJournal();

// Configure byte overlay with journal
overlay.Journal = journal;

// Perform operations
overlay.OverwriteByte(0x100, 0xFF);
overlay.InsertBytes(0x200, new byte[] { 0xAA, 0xBB });

// Undo last operation
if (journal.CanUndo)
{
    journal.Undo(overlay);
}

// Redo operation
if (journal.CanRedo)
{
    journal.Redo(overlay);
}

// Batch operations (single undo unit)
journal.BeginBatch();
overlay.OverwriteByte(0x100, 0x11);
overlay.OverwriteByte(0x101, 0x22);
overlay.OverwriteByte(0x102, 0x33);
journal.EndBatch();
```

### Search Operations

```csharp
var searchService = new HexSearchService();

// Search for hex pattern
var pattern = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
var offset = searchService.FindNext(lineReader, pattern, startOffset);

if (offset != -1)
{
    // Found at offset
    HexView.MoveCaretTo(offset);
}

// Search with wildcards (? = any nibble)
var wildcardPattern = "DE AD ?? EF";
var maskPattern = searchService.ParseHexBytesWithWildcards(wildcardPattern);
offset = searchService.FindNext(lineReader, maskPattern.pattern,
                                 startOffset, maskPattern.mask);

// Text search
var textBytes = Encoding.ASCII.GetBytes("Hello");
offset = searchService.FindNext(lineReader, textBytes, 0);

// Navigate to address
var success = searchService.TryNavigateToAddress(HexView, "0x1234");
```

### Find and Replace

```csharp
var searchService = new HexSearchService();
var overlay = (ByteOverlay)((ByteOverlayLineReader)HexView.LineReader).Overlay;

// Replace next occurrence
var findPattern = new byte[] { 0xAA, 0xBB };
var replacePattern = new byte[] { 0xCC, 0xDD };

var offset = searchService.ReplaceNext(
    overlay,
    findPattern,
    replacePattern,
    startOffset
);

// Replace all occurrences
var count = searchService.ReplaceAll(
    overlay,
    findPattern,
    replacePattern
);

Console.WriteLine($"Replaced {count} occurrences");
```

### Selection Operations

```csharp
var selectionService = new SelectionService();

// Fill selection with specific byte
selectionService.FillSelection(overlay, 0xFF,
    HexView.SelectionStart, HexView.SelectionLength);

// Zero-fill selection
selectionService.ZeroFillSelection(overlay,
    HexView.SelectionStart, HexView.SelectionLength);

// Increment all bytes in selection
selectionService.IncrementSelection(overlay,
    HexView.SelectionStart, HexView.SelectionLength);

// Randomize selection
selectionService.RandomizeSelection(overlay,
    HexView.SelectionStart, HexView.SelectionLength, seed: 12345);

// Copy as hex
var hexString = selectionService.CopySelectionAsHex(
    lineReader, HexView.SelectionStart, HexView.SelectionLength, spaced: true);

// Copy as ASCII
var asciiString = selectionService.CopySelectionAsAscii(
    lineReader, HexView.SelectionStart, HexView.SelectionLength, Encoding.ASCII);

// Paste hex bytes
var hexInput = "DE AD BE EF";
selectionService.PasteHexBytes(overlay, hexInput,
    HexView.CaretOffset, insertMode: false);

// Paste ASCII text
var textInput = "Hello World";
selectionService.PasteAsciiBytes(overlay, textInput,
    HexView.CaretOffset, Encoding.UTF8, insertMode: false);
```

### Navigation and Bookmarks

```csharp
var navigationService = new NavigationService();

// Add bookmark
navigationService.AddBookmark(0x1000, "Important location");

// List bookmarks
var bookmarks = navigationService.ListBookmarks();

// Remove bookmark
navigationService.RemoveBookmark(0x1000);

// Navigate with history
navigationService.Visit(HexView.CaretOffset);
HexView.MoveCaretTo(0x2000);

// Go back
if (navigationService.CanGoBack)
{
    var previousOffset = navigationService.GoBack();
    HexView.MoveCaretTo(previousOffset);
}

// Go forward
if (navigationService.CanGoForward)
{
    var nextOffset = navigationService.GoForward();
    HexView.MoveCaretTo(nextOffset);
}
```

### Diff Mode

```csharp
// Create two hex view controls
var leftReader = new MemoryMappedLineReader("file1.bin");
var rightReader = new MemoryMappedLineReader("file2.bin");

var leftFormatter = new HexFormatter(leftReader);
var rightFormatter = new HexFormatter(rightReader);

LeftHexView.LineReader = leftReader;
LeftHexView.HexFormatter = leftFormatter;

RightHexView.LineReader = rightReader;
RightHexView.HexFormatter = rightFormatter;

// Compute differences
var differences = ComputeDifferences(leftReader, rightReader);

// Provide difference highlighting
LeftHexView.DifferencesProvider = () => differences;
RightHexView.DifferencesProvider = () => differences;

// Synchronized scrolling
LeftHexView.PropertyChanged += (s, e) =>
{
    if (e.Property.Name == nameof(HexViewControl.CaretOffset))
    {
        RightHexView.MoveCaretTo(LeftHexView.CaretOffset);
    }
};
```

### Saving Files

```csharp
var saveService = new SaveService();
var overlay = (ByteOverlay)((ByteOverlayLineReader)HexView.LineReader).Overlay;

// Save to new file
await saveService.SaveAs(overlay, "output.bin");

// Export patch file
var patchContent = saveService.ExportPatch(overlay);
await File.WriteAllTextAsync("changes.patch", patchContent);

// Patch format example:
// OW 00000100 FF        # Overwrite at 0x100 with 0xFF
// IN 00000200 DEADBEEF  # Insert DEADBEEF at 0x200
// DL 00000300 00000004  # Delete 4 bytes at 0x300
```

### Visual Customization

```csharp
// Customize colors
HexView.CaretBrush = new SolidColorBrush(Colors.Red);
HexView.EditedBrush = new SolidColorBrush(Colors.Orange);
HexView.SelectionBrush = new SolidColorBrush(Color.FromArgb(80, 0, 120, 215));
HexView.MatchBrush = new SolidColorBrush(Colors.Yellow);
HexView.DiffBrush = new SolidColorBrush(Color.FromArgb(80, 144, 238, 144));

// Font customization
HexView.FontFamily = new FontFamily("Consolas");
HexView.FontSize = 14;

// Zoom support
HexView.PointerWheelChanged += (s, e) =>
{
    if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
    {
        HexView.FontSize += e.Delta.Y > 0 ? 1 : -1;
        e.Handled = true;
    }
};
```

### Event Handling

```csharp
// Caret position changed
HexView.CaretMoved += (offset) =>
{
    Console.WriteLine($"Caret moved to: 0x{offset:X}");

    // Update status bar
    StatusText.Text = $"Offset: 0x{offset:X8} ({offset})";

    // Read byte at caret
    if (HexView.TryGetByteAt(offset, out var value))
    {
        StatusText.Text += $" | Value: 0x{value:X2} ({value})";
    }
};

// Selection changed
HexView.SelectionChanged += () =>
{
    var start = HexView.SelectionStart;
    var length = HexView.SelectionLength;

    Console.WriteLine($"Selection: 0x{start:X} - 0x{(start + length):X} ({length} bytes)");
};

// Byte written (for external edit tracking)
HexView.ByteWriteAction = (offset, oldValue, newValue) =>
{
    Console.WriteLine($"Byte at 0x{offset:X}: 0x{oldValue:X2} -> 0x{newValue:X2}");
};
```

### Custom Data Source

```csharp
// Implement ILineReader for custom data sources
public class CustomLineReader : ILineReader
{
    private readonly byte[] _data;

    public long Length => _data.Length;

    public CustomLineReader(byte[] data)
    {
        _data = data;
    }

    public void GetLine(long offset, Span<byte> buffer)
    {
        var length = Math.Min(buffer.Length, _data.Length - offset);
        _data.AsSpan((int)offset, (int)length).CopyTo(buffer);
    }

    public int Read(long offset)
    {
        return offset < _data.Length ? _data[offset] : -1;
    }
}

// Use custom reader
var customReader = new CustomLineReader(myData);
var formatter = new HexFormatter(customReader);
HexView.LineReader = customReader;
HexView.HexFormatter = formatter;
```

## Keyboard Shortcuts

### File Operations
- `Ctrl+O` - Open file
- `Ctrl+S` - Save file
- `Ctrl+Shift+S` - Save As

### Editing
- `Ctrl+Z` - Undo
- `Ctrl+Y` / `Ctrl+Shift+Z` - Redo
- `Delete` - Delete selection (insert mode)
- `Insert` - Toggle insert/overwrite mode

### Clipboard
- `Ctrl+C` - Copy selection as hex
- `Ctrl+Shift+C` - Copy selection as ASCII
- `Ctrl+V` - Paste hex bytes
- `Ctrl+Shift+V` - Paste ASCII text

### Search & Navigation
- `Ctrl+F` - Focus search
- `Ctrl+H` - Focus replace
- `Ctrl+G` - Go to address
- `F3` - Find next
- `Shift+F3` - Find previous
- `Alt+Left` - Navigate back
- `Alt+Right` - Navigate forward
- `Ctrl+B` - Add bookmark

### Selection & View
- `Ctrl+A` - Select all
- `Ctrl++` / `Ctrl+=` - Zoom in
- `Ctrl+-` - Zoom out
- `Ctrl+0` - Reset zoom

### Navigation Keys
- `Arrow Keys` - Move caret
- `Home` - Move to line start
- `End` - Move to line end
- `Page Up` / `Page Down` - Scroll page
- `Ctrl+Home` - Go to file start
- `Ctrl+End` - Go to file end

## Architecture

HexView uses a layered architecture for flexibility and performance:

```
┌─────────────────────────────────────┐
│       HexViewControl (UI)           │
│  - Rendering & User Interaction     │
└─────────────────┬───────────────────┘
                  │
┌─────────────────┴───────────────────┐
│      Services Layer                 │
│  - HexFormatter                     │
│  - HexSearchService                 │
│  - SelectionService                 │
│  - NavigationService                │
│  - EditJournal                      │
│  - SaveService                      │
└─────────────────┬───────────────────┘
                  │
┌─────────────────┴───────────────────┐
│      Data Layer                     │
│  - ByteOverlay (Edit Tracking)      │
│  - ByteOverlayLineReader            │
│  - MemoryMappedLineReader           │
│  - ILineReader (Interface)          │
└─────────────────────────────────────┘
```

### Key Design Patterns

- **Interface-based design**: Easy to extend with custom implementations
- **Piece table editing**: Efficient edit tracking without modifying original file
- **Memory-mapped files**: Large file support without loading into RAM
- **Virtual scrolling**: Only render visible lines for performance
- **Event-driven updates**: Responsive UI with minimal overhead

## Performance Considerations

- Memory-mapped files enable editing multi-gigabyte files
- Chunked operations (64KB default) for optimal I/O
- Virtual scrolling renders only visible content
- Piece table keeps edits separate from original data
- Efficient pattern search with overlap detection
- Lazy computation of differences in diff mode

## Requirements

- .NET 9.0 or later
- Avalonia 11.2 or later

## Building from Source

```bash
git clone https://github.com/wieslawsoltes/HexView.git
cd HexView
dotnet build
```

### Running Samples

```bash
# Desktop sample
dotnet run --project samples/HexView.Desktop

# Console sample (requires compatible terminal)
dotnet run --project samples/HexView.Console
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Acknowledgments

- Built with [Avalonia UI](https://avaloniaui.net/)
- Inspired by classic hex editors like HxD and 010 Editor

## Support

- Report issues: [GitHub Issues](https://github.com/wieslawsoltes/HexView/issues)
- Discussions: [GitHub Discussions](https://github.com/wieslawsoltes/HexView/discussions)
