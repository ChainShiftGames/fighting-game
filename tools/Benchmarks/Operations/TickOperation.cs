using Sim.Composition;
using Sim.State;

namespace Benchmarks.Operations;

/// <summary>One logic frame of the real match: the budget in ADR-0003 (≤ 1 ms on a Steam Deck, no allocation).</summary>
internal struct TickOperation : IBenchmarkOperation
{
    private readonly MatchSimulation _simulation;
    private readonly InputSequence _inputs;
    private readonly MatchState _start;
    private MatchState _state;

    public TickOperation(MatchSimulation simulation, InputSequence inputs)
    {
        _simulation = simulation;
        _inputs = inputs;
        _start = simulation.StartState();
        _state = _start;
    }

    public readonly string Name => "Tick";

    public void Run()
    {
        // A decided match only idles, so start over to keep measuring fighting frames. Rare enough (once per
        // thousands of calls) not to show in the per-call numbers.
        if (_state.Round.Phase == RoundPhase.MatchOver)
        {
            _state = _start;
        }

        _simulation.Tick(ref _state, _inputs.Player1At(_state.Frame), _inputs.Player2At(_state.Frame));
    }
}
