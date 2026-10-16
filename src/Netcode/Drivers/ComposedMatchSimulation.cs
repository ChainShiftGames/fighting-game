using Sim.Composition;
using Sim.Input;
using Sim.State;

namespace Netcode.Drivers;

/// <summary>Lets match drivers run the real game logic: a match composed from data by <see cref="MatchSimulation"/>.</summary>
public sealed class ComposedMatchSimulation : IMatchSimulation<MatchState>
{
    private readonly MatchSimulation _match;

    public ComposedMatchSimulation(MatchSimulation match)
    {
        _match = match;
    }

    public MatchState CreateInitialState() => _match.StartState();

    public void Tick(ref MatchState state, InputWord player1, InputWord player2) =>
        _match.Tick(ref state, player1, player2);
}
