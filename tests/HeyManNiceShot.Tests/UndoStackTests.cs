using HeyManNiceShot.Editing;
using Xunit;

namespace HeyManNiceShot.Tests;

public class UndoStackTests
{
    [Fact]
    public void Push_StoresValue()
    {
        var s = new UndoStack<int>();
        s.Push(1);
        Assert.Equal(1, s.Current);
        Assert.False(s.CanUndo);
        Assert.False(s.CanRedo);
    }

    [Fact]
    public void Undo_RestoresPrevious()
    {
        var s = new UndoStack<int>();
        s.Push(1);
        s.Push(2);
        Assert.Equal(2, s.Current);
        Assert.Equal(1, s.Undo());
        Assert.True(s.CanRedo);
    }

    [Fact]
    public void Redo_AfterUndo_Reinstates()
    {
        var s = new UndoStack<int>();
        s.Push(1);
        s.Push(2);
        s.Undo();
        Assert.Equal(2, s.Redo());
    }

    [Fact]
    public void Push_AfterUndo_DiscardsRedoHistory()
    {
        var s = new UndoStack<int>();
        s.Push(1);
        s.Push(2);
        s.Undo();
        s.Push(3);
        Assert.False(s.CanRedo);
        Assert.Equal(3, s.Current);
    }

    [Fact]
    public void Push_BeyondLimit_TrimsOldest()
    {
        var s = new UndoStack<int>(limit: 3);
        s.Push(1);
        s.Push(2);
        s.Push(3);
        s.Push(4);
        Assert.Equal(4, s.Current);
        s.Undo();
        s.Undo();
        Assert.Equal(2, s.Current);
        Assert.False(s.CanUndo);
    }

    [Fact]
    public void Empty_CurrentThrows()
    {
        var s = new UndoStack<int>();
        Assert.Throws<InvalidOperationException>(() => s.Current);
    }
}
