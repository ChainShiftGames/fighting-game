using Sim.State;

namespace Benchmarks.Operations;

/// <summary>The state checksum compared between both PCs to detect desyncs.</summary>
internal struct ChecksumOperation<TState> : IBenchmarkOperation
    where TState : unmanaged
{
    private readonly TState _state;
    private readonly string _name;

    // Kept so the JIT can't drop the hash as unused.
    private ulong _lastChecksum;

    public ChecksumOperation(string stateName, TState state)
    {
        _state = state;
        _name = $"Checksum ({stateName})";
    }

    public readonly string Name => _name;

    public readonly ulong LastChecksum => _lastChecksum;

    public void Run()
    {
        _lastChecksum = StateChecksum.Compute(in _state);
    }
}
