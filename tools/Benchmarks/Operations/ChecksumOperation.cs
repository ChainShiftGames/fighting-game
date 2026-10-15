using Sim.State;
using Sim.Toy;

namespace Benchmarks.Operations;

/// <summary>The state checksum compared between both PCs to detect desyncs.</summary>
internal struct ChecksumOperation : IBenchmarkOperation
{
    private readonly ToyState _state;

    // Kept so the JIT can't drop the hash as unused.
    private ulong _lastChecksum;

    public ChecksumOperation(ToySimulation simulation)
    {
        _state = simulation.CreateInitialState();
    }

    public readonly string Name => "Checksum";

    public readonly ulong LastChecksum => _lastChecksum;

    public void Run()
    {
        _lastChecksum = StateChecksum.Compute(in _state);
    }
}
