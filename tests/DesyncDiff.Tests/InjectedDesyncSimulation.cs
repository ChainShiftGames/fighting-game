using Netcode.Drivers;
using Sim.Input;
using Sim.State;

namespace DesyncDiff.Tests;

/// <summary>The real match with one deliberate bug: on one frame it adds 1 to P2's health, and nothing else.</summary>
internal sealed class InjectedDesyncSimulation : IMatchSimulation<MatchState>
{
    public const string ChangedField = "Fighters[1].Health";

    private readonly IMatchSimulation<MatchState> _simulation;
    private readonly int _desyncFrame;
    private int _frame;

    public InjectedDesyncSimulation(IMatchSimulation<MatchState> simulation, int desyncFrame)
    {
        _simulation = simulation;
        _desyncFrame = desyncFrame;
    }

    public MatchState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref MatchState state, InputWord player1, InputWord player2)
    {
        _simulation.Tick(ref state, player1, player2);
        if (++_frame == _desyncFrame)
        {
            state.Fighters[1].Health++;
        }
    }
}
