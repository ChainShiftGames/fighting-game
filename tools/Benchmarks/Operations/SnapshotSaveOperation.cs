namespace Benchmarks.Operations;

/// <summary>Saving a snapshot: copying the live state into the next slot of a rollback-window ring.</summary>
internal struct SnapshotSaveOperation<TState> : IBenchmarkOperation
    where TState : unmanaged
{
    private readonly TState[] _ring;
    private readonly TState _live;
    private readonly string _name;
    private int _nextSlot;

    public SnapshotSaveOperation(string stateName, TState live, int rollbackFrames)
    {
        _ring = new TState[rollbackFrames];
        _live = live;
        _name = $"Snapshot save ({stateName})";
    }

    public readonly string Name => _name;

    public void Run()
    {
        _ring[_nextSlot] = _live;
        _nextSlot = (_nextSlot + 1) % _ring.Length;
    }
}
