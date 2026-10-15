using Netcode.Drivers;
using Sim.Input;
using Sim.Toy;

namespace Netcode.Tests.Drivers;

/// <summary>
/// The toy simulation with a deliberate determinism bug: it keeps a counter outside the game state, so running the
/// same frame twice (as a rollback does) gives a different result.
/// </summary>
internal sealed class NonDeterministicSimulation : IMatchSimulation<ToyState>
{
    private readonly ToyMatchSimulation _simulation = new(new ToySimulation(ToyRules.Default));
    private int _tickCalls;

    public ToyState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref ToyState state, InputWord player1, InputWord player2)
    {
        _simulation.Tick(ref state, player1, player2);
        state.Fighters[0].PositionX += _tickCalls++;
    }
}
