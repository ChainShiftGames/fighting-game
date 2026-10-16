using System;

namespace Benchmarks.Operations;

/// <summary>Loading a snapshot: copying a slot of the rollback-window ring back into the live state.</summary>
internal struct SnapshotLoadOperation<TState> : IBenchmarkOperation
    where TState : unmanaged
{
    private readonly TState[] _ring;
    private readonly string _name;
    private TState _live;
    private int _nextSlot;

    public SnapshotLoadOperation(string stateName, TState saved, int rollbackFrames)
    {
        _ring = new TState[rollbackFrames];
        _ring.AsSpan().Fill(saved);
        _name = $"Snapshot load ({stateName})";
    }

    public readonly string Name => _name;

    // Read so the copy counts as used.
    public readonly TState Live => _live;

    public void Run()
    {
        _live = _ring[_nextSlot];
        _nextSlot = (_nextSlot + 1) % _ring.Length;
    }
}
