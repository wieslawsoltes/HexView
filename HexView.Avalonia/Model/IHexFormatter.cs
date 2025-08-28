// HexView control for Avalonia.
// Copyright (C) 2025  Wiesław Šoltés
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
using System.Text;

namespace HexView.Avalonia.Model;

public interface IHexFormatter
{
    long Length { get; }
    long Lines { get; }
    int Width { get; set; }
    int OffsetPadding { get; }
    void AddLine(byte[] bytes, long lineNumber, StringBuilder sb, int toBase);

    // Column configuration
    int GroupSize { get; set; }
    bool ShowGroupSeparator { get; set; }
    int AddressWidthOverride { get; set; }

    // ASCII pane configuration
    Encoding Encoding { get; set; }
    bool UseControlGlyph { get; set; }
    char ControlGlyph { get; set; }
}
