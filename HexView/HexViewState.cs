using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace HexView;

public class HexViewState
{
    private FileInfo _info;
    private MemoryMappedFile _file;
    private MemoryMappedViewAccessor _accessor;
    private int _width;
    private long _lines;

    public long Lines => _lines;
    
    public HexViewState(string path)
    {
        _info = new FileInfo(path); 
        _file = MemoryMappedFile.CreateFromFile(path); 
        _accessor = _file.CreateViewAccessor(0, _info.Length);
        _width = 16; // 8m 16, 24, 32
        _lines = _info.Length / _width;
    }

    public byte[] GetLine(long lineNumber)
    {
        var bytes = new byte[_width];

        var offset = lineNumber * _width;

        Console.Write($"{offset:X8}: ");

        for (var j = 0; j < _width; j++)
        {
            var position = offset + j;

                var isSplit = j > 0 && j % 8 == 0;
                if (isSplit)
                {
                    Console.Write("| ");
                }
                
                if (position < _info.Length)
                {
                    bytes[j] = _accessor.ReadByte(position);
                    Console.Write($"{bytes[j]:X2}");
                }
                else
                {
                    Console.Write("  ");
                }

                if (!isSplit)
                {
                    Console.Write(' ');
                }
        }

        Console.Write(" | ");

        for (int j = 0; j < _width; j++)
        {
            var c = (char)bytes[j];

            if (char.IsControl(c))
            {
                Console.Write(' ');
            }
            else
            {
                Console.Write(c); 
            }
        }

        Console.WriteLine();

        return bytes;
    }
}
