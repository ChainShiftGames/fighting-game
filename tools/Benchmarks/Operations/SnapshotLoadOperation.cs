using System;
using Sim.Toy;

namespace Benchmarks.Operations;

/// <summary>Loading a snapshot: copying a slot of the rollback-window ring back into the live state.</summary>
internal struct SnapshotLoadOperation : IBenchmarkOperation
{
    private readonly ToyState[] _ring;
    private ToyState _live;
    private int _nextSlot;

    public SnapshotLoadOperation(ToySimulation simulation, int rollbackFrames)
    {
        _ring = new ToyState[rollbackFrames];
        _ring.AsSpan().Fill(simulation.CreateInitialState());
    }

    public readonly string Name => "Snapshot load";

    // Read so the copy counts as used.
    public readonly int LiveFrame => _live.Frame;

    public void Run()
    {
        _live = _ring[_nextSlot];
        _nextSlot = (_nextSlot + 1) % _ring.Length;
    }
}
