using Sim.Toy;

namespace Benchmarks.Operations;

/// <summary>One logic frame: the budget in ADR-0003 (≤ 1 ms on a Steam Deck, no allocation).</summary>
internal struct TickOperation : IBenchmarkOperation
{
    private readonly ToySimulation _simulation;
    private readonly InputSequence _inputs;
    private ToyState _state;

    public TickOperation(ToySimulation simulation, InputSequence inputs)
    {
        _simulation = simulation;
        _inputs = inputs;
        _state = simulation.CreateInitialState();
    }

    public readonly string Name => "Tick";

    public void Run()
    {
        _simulation.Tick(ref _state, _inputs.Player1At(_state.Frame), _inputs.Player2At(_state.Frame));
    }
}
