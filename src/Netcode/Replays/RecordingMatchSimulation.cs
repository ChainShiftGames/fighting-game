using Netcode.Drivers;
using Sim.Input;
using Sim.State;

namespace Netcode.Replays;

/// <summary>Records a match for a replay while it runs: wraps the game logic that any match driver advances.</summary>
/// <remarks>
/// Recording here rather than around the match driver sees both players' inputs, including the other PC's in an
/// online match, and sees every rollback (the driver hides those). The frame comes from the state being ticked, so
/// a resimulated frame overwrites its earlier, predicted inputs (<see cref="ReplayRecorder"/>).
/// </remarks>
public sealed class RecordingMatchSimulation : IMatchSimulation<MatchState>
{
    private readonly IMatchSimulation<MatchState> _simulation;
    private readonly ReplayRecorder _recorder;

    public RecordingMatchSimulation(IMatchSimulation<MatchState> simulation, ReplayRecorder recorder)
    {
        _simulation = simulation;
        _recorder = recorder;
    }

    public MatchState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref MatchState state, InputWord player1, InputWord player2)
    {
        _recorder.Record(state.Frame, player1, player2);
        _simulation.Tick(ref state, player1, player2);
    }
}
