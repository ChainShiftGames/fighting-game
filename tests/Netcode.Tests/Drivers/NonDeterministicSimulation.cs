using Netcode.Drivers;
using Netcode.Tests.Matches;
using Sim.Input;
using Sim.Math;
using Sim.State;

namespace Netcode.Tests.Drivers;

/// <summary>
/// The Testman mirror with a deliberate determinism bug: it keeps a counter outside the game state, so running the
/// same frame twice (as a rollback does) gives a different result.
/// </summary>
internal sealed class NonDeterministicSimulation : IMatchSimulation<MatchState>
{
    private readonly ComposedMatchSimulation _simulation = new(TestmanMirror.Create());
    private int _tickCalls;

    public MatchState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref MatchState state, InputWord player1, InputWord player2)
    {
        _simulation.Tick(ref state, player1, player2);
        IntVector2 position = state.Fighters[0].Position;
        state.Fighters[0].Position = position with { X = position.X + _tickCalls++ };
    }
}
