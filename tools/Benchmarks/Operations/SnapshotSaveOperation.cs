using Sim.Toy;

namespace Benchmarks.Operations;

/// <summary>Saving a snapshot: copying the live state into the next slot of a rollback-window ring.</summary>
internal struct SnapshotSaveOperation : IBenchmarkOperation
{
    private readonly ToyState[] _ring;
    private readonly ToyState _live;
    private int _nextSlot;

    public SnapshotSaveOperation(ToySimulation simulation, int rollbackFrames)
    {
        _ring = new ToyState[rollbackFrames];
        _live = simulation.CreateInitialState();
    }

    public readonly string Name => "Snapshot save";

    public void Run()
    {
        _ring[_nextSlot] = _live;
        _nextSlot = (_nextSlot + 1) % _ring.Length;
    }
}
