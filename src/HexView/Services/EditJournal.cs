// HexView control for Avalonia.
// Copyright (C) 2025  Wiesław Šoltés

using System;
using System.Collections.Generic;

namespace HexView.Avalonia.Services;

public enum EditOpType
{
    Overwrite,
    Insert,
    Delete,
    Replace,
    Batch
}

public class EditOperation
{
    public EditOpType Type { get; set; }
    public long Offset { get; set; }
    public byte[] OldData { get; set; } = Array.Empty<byte>();
    public byte[] NewData { get; set; } = Array.Empty<byte>();
    public List<EditOperation> Children { get; set; } = new();
}

public class EditJournal
{
    private readonly Stack<EditOperation> _undo = new();
    private readonly Stack<EditOperation> _redo = new();
    private readonly Stack<EditOperation> _batchStack = new();

    public void Record(EditOperation op)
    {
        if (_batchStack.Count > 0)
        {
            _batchStack.Peek().Children.Add(op);
        }
        else
        {
            _undo.Push(op);
            _redo.Clear();
        }
    }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void BeginBatch()
    {
        _batchStack.Push(new EditOperation { Type = EditOpType.Batch });
    }

    public void EndBatch()
    {
        if (_batchStack.Count == 0) return;
        var batch = _batchStack.Pop();
        if (batch.Children.Count == 0)
            return;
        if (_batchStack.Count > 0)
        {
            _batchStack.Peek().Children.Add(batch);
        }
        else
        {
            _undo.Push(batch);
            _redo.Clear();
        }
    }

    public void Undo(ByteOverlay overlay)
    {
        if (!CanUndo) return;
        var op = _undo.Pop();
        ApplyInverse(overlay, op);
        _redo.Push(op);
    }

    public void Redo(ByteOverlay overlay)
    {
        if (!CanRedo) return;
        var op = _redo.Pop();
        Apply(overlay, op);
        _undo.Push(op);
    }

    private static void Apply(ByteOverlay overlay, EditOperation op)
    {
        switch (op.Type)
        {
            case EditOpType.Overwrite:
                for (int i = 0; i < op.NewData.Length; i++) overlay.OverwriteByte(op.Offset + i, op.NewData[i]);
                break;
            case EditOpType.Insert:
                overlay.InsertBytes(op.Offset, op.NewData);
                break;
            case EditOpType.Delete:
                overlay.DeleteRange(op.Offset, op.OldData.Length);
                break;
            case EditOpType.Replace:
                overlay.ReplaceRange(op.Offset, op.OldData.Length, op.NewData);
                break;
            case EditOpType.Batch:
                foreach (var child in op.Children)
                {
                    Apply(overlay, child);
                }
                break;
        }
    }

    private static void ApplyInverse(ByteOverlay overlay, EditOperation op)
    {
        switch (op.Type)
        {
            case EditOpType.Overwrite:
                for (int i = 0; i < op.OldData.Length; i++) overlay.OverwriteByte(op.Offset + i, op.OldData[i]);
                break;
            case EditOpType.Insert:
                overlay.DeleteRange(op.Offset, op.NewData.Length);
                break;
            case EditOpType.Delete:
                overlay.InsertBytes(op.Offset, op.OldData);
                break;
            case EditOpType.Replace:
                overlay.ReplaceRange(op.Offset, op.NewData.Length, op.OldData);
                break;
            case EditOpType.Batch:
                for (int i = op.Children.Count - 1; i >= 0; i--)
                {
                    ApplyInverse(overlay, op.Children[i]);
                }
                break;
        }
    }
}
