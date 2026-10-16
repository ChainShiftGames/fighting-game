using FightingGame.Presentation.Events;
using Netcode.Drivers;
using Sim.Events;
using Sim.Input;
using Sim.Math;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Presentation.Events;

public sealed class EventObservingSimulationTests
{
    private static readonly InputWord _attack = new(1);

    private readonly RecordingEffect _effect = new();
    private readonly EventObservingSimulation _simulation;

    public EventObservingSimulationTests()
    {
        _simulation = new EventObservingSimulation(new HitOnInput(), new FrameEventReconciler(_effect, 8));
    }

    [Fact]
    public void AReSimulatedHitPlaysOnce()
    {
        MatchState state = _simulation.CreateInitialState();
        MatchState beforeHit = state;
        _simulation.Tick(ref state, _attack, default);
        _simulation.Tick(ref state, default, default);

        // Rollback to before the hit; the same inputs run again, as when the prediction was right.
        state = beforeHit;
        _simulation.Tick(ref state, _attack, default);
        _simulation.Tick(ref state, default, default);

        Assert.Single(_effect.Played);
        Assert.Empty(_effect.Cancelled);
    }

    [Fact]
    public void AHitThatRollbackRemovesIsCancelled()
    {
        MatchState state = _simulation.CreateInitialState();
        MatchState beforeHit = state;
        _simulation.Tick(ref state, _attack, default);
        _simulation.Tick(ref state, default, default);

        // Rollback: the predicted attack input was wrong.
        state = beforeHit;
        _simulation.Tick(ref state, default, default);
        _simulation.Tick(ref state, default, default);

        FrameEvent hit = Assert.Single(_effect.Played);
        Assert.Equal(hit.Id, Assert.Single(_effect.Cancelled).Id);
        Assert.Equal(0, hit.Frame);
    }

    [Fact]
    public void LeavesTheStateAsTheInnerSimulationMadeIt()
    {
        MatchState observed = _simulation.CreateInitialState();
        MatchState plain = observed;

        _simulation.Tick(ref observed, _attack, default);
        new HitOnInput().Tick(ref plain, _attack, default);

        Assert.Equal(plain.Frame, observed.Frame);
        Assert.Equal(plain.Events.OfFrame(0).ToArray(), observed.Events.OfFrame(0).ToArray());
    }

    // Stand-in for the game logic: P1 pressing anything hits on that frame.
    private sealed class HitOnInput : IMatchSimulation<MatchState>
    {
        public MatchState CreateInitialState() => default;

        public void Tick(ref MatchState state, InputWord player1, InputWord player2)
        {
            if (player1 != default)
            {
                state.Events.Emit(state.Frame, FrameEventKind.Hit, 0, new IntVector2(1_000, 50_000), detail: 1);
            }

            state.Frame++;
        }
    }
}
