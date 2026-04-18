namespace HeyManNiceShot.Editing;

public sealed class UndoStack<T>
{
    private readonly List<T> _history = new();
    private int _index = -1;
    private readonly int _limit;

    public UndoStack(int limit = 64)
    {
        _limit = Math.Max(2, limit);
    }

    public T Current => _index < 0
        ? throw new InvalidOperationException("UndoStack is empty.")
        : _history[_index];

    public bool CanUndo => _index > 0;
    public bool CanRedo => _index >= 0 && _index < _history.Count - 1;

    public void Push(T snapshot)
    {
        if (_index < _history.Count - 1)
            _history.RemoveRange(_index + 1, _history.Count - _index - 1);

        _history.Add(snapshot);
        _index = _history.Count - 1;

        while (_history.Count > _limit)
        {
            _history.RemoveAt(0);
            _index--;
        }
    }

    public T Undo()
    {
        if (!CanUndo) throw new InvalidOperationException("Nothing to undo.");
        _index--;
        return _history[_index];
    }

    public T Redo()
    {
        if (!CanRedo) throw new InvalidOperationException("Nothing to redo.");
        _index++;
        return _history[_index];
    }
}
