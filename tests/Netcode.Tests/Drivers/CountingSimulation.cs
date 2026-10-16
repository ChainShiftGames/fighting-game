using Netcode.Drivers;
using Netcode.Tests.Matches;
using Sim.Input;
using Sim.State;

namespace Netcode.Tests.Drivers;

/// <summary>The Testman mirror, counting how often a frame is run, to prove resimulation actually happens.</summary>
internal sealed class CountingSimulation : IMatchSimulation<MatchState>
{
    private readonly ComposedMatchSimulation _simulation = new(TestmanMirror.Create());

    public int TickCalls { get; private set; }

    public MatchState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref MatchState state, InputWord player1, InputWord player2)
    {
        _simulation.Tick(ref state, player1, player2);
        TickCalls++;
    }
}
