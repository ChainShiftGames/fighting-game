using Netcode.Drivers;
using Sim.Input;
using Sim.State;

namespace DesyncDiff.Tests;

/// <summary>The real match with one deliberate bug: on one frame it adds 1 to P2's health, and nothing else.</summary>
/// <remarks>
/// Keyed on the state's frame, not on calls: online, a rollback reruns the frame from the snapshot before it and must
/// hit the same bug again.
/// </remarks>
internal sealed class InjectedDesyncSimulation : IMatchSimulation<MatchState>
{
    public const string ChangedField = "Fighters[1].Health";

    private readonly IMatchSimulation<MatchState> _simulation;
    private readonly int _desyncFrame;

    public InjectedDesyncSimulation(IMatchSimulation<MatchState> simulation, int desyncFrame)
    {
        _simulation = simulation;
        _desyncFrame = desyncFrame;
    }

    public MatchState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref MatchState state, InputWord player1, InputWord player2)
    {
        _simulation.Tick(ref state, player1, player2);
        if (state.Frame == _desyncFrame)
        {
            state.Fighters[1].Health++;
        }
    }
}
