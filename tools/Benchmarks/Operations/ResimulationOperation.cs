using Sim.Composition;
using Sim.State;

namespace Benchmarks.Operations;

/// <summary>
/// The rollback worst case (ADR-0003): restore a mid-match snapshot, then re-run the whole rollback window of frames.
/// </summary>
internal struct ResimulationOperation : IBenchmarkOperation
{
    private readonly MatchSimulation _simulation;
    private readonly InputSequence _inputs;
    private readonly int _rollbackFrames;
    private readonly MatchState _snapshot;
    private MatchState _state;

    public ResimulationOperation(MatchSimulation simulation, MatchState snapshot, InputSequence inputs, int rollbackFrames)
    {
        _simulation = simulation;
        _inputs = inputs;
        _rollbackFrames = rollbackFrames;
        _snapshot = snapshot;
        _state = _snapshot;
        Name = $"Resimulate {rollbackFrames} frames";
    }

    public string Name { get; }

    public void Run()
    {
        _state = _snapshot;
        for (int i = 0; i < _rollbackFrames; i++)
        {
            _simulation.Tick(ref _state, _inputs.Player1At(_state.Frame), _inputs.Player2At(_state.Frame));
        }
    }
}
