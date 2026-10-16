using Netcode.Drivers;
using Sim.Input;
using Sim.State;

namespace FightingGame.Presentation.Events;

/// <summary>
/// Runs the match logic for a driver and hands each simulated frame's events to presentation, re-simulations
/// included, so effects follow rollbacks without the driver knowing about them.
/// </summary>
/// <remarks>
/// The driver only shows its newest state, but a rollback re-runs several frames inside one call; this sees every
/// one of them. It only reads the state after the inner simulation's tick (ARCHITECTURE.md §9).
/// </remarks>
public sealed class EventObservingSimulation : IMatchSimulation<MatchState>
{
    private readonly IMatchSimulation<MatchState> _simulation;
    private readonly FrameEventReconciler _reconciler;

    public EventObservingSimulation(IMatchSimulation<MatchState> simulation, FrameEventReconciler reconciler)
    {
        _simulation = simulation;
        _reconciler = reconciler;
    }

    public MatchState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref MatchState state, InputWord player1, InputWord player2)
    {
        _simulation.Tick(ref state, player1, player2);
        int simulatedFrame = state.Frame - 1;
        _reconciler.Simulated(simulatedFrame, state.Events.OfFrame(simulatedFrame));
    }
}
