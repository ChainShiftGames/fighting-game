using Sim.Events;
using Sim.Input;
using Sim.Math;
using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;
using Xunit;

namespace Sim.Tests.Pipeline;

/// <summary>The core capabilities reserved by ADR-0011, each driven by a test-only module's core requests.</summary>
public sealed class CoreCapabilityTests
{
    private const int Player1 = 0;
    private const int Player2 = 1;

    [Fact]
    public void TimeScaleSlowsOnlyTheChosenFighterForItsDuration()
    {
        var slowdown = OnFrame(0, context => context.RequestTimeScale(
            Player2, new TimeScaleRequest { Numerator = 1, Denominator = 2, Frames = 6 }));
        var counter = new AdvanceCountingStage();
        MatchState state = Run(new TickStages { Movement = counter }, frames: 10, slowdown);

        // Requested before the state machines, so frame 0 is already slowed: 3 of the first 6 frames, then 4 normal.
        Assert.Equal(10, counter.AdvancedFrames(Player1));
        Assert.Equal(3 + 4, counter.AdvancedFrames(Player2));
        Assert.Equal(default, state.Fighters[Player2].Timing);
    }

    [Fact]
    public void TimeScaleAlsoStretchesTheFightersHitstop()
    {
        var module = OnFrame(0, context =>
        {
            context.RequestTimeScale(Player1, new TimeScaleRequest { Numerator = 1, Denominator = 2, Frames = 20 });
            context.RequestHitstopOverride(Player1, new HitstopOverrideRequest { Frames = 2 });
        });
        var counter = new AdvanceCountingStage();
        Run(new TickStages { Movement = counter }, frames: 6, module);

        // Own frames on match frames 1, 3, 5; the first two are hitstop, so it first advances on frame 5.
        Assert.Equal([false, false, false, false, false, true], counter.Frames.ConvertAll(frame => frame.Player1));
    }

    [Fact]
    public void GlobalFreezeStopsEveryoneButTheExemptedFighter()
    {
        var superFlash = OnFrame(1, context => context.RequestFreeze(
            new FreezeRequest { Frames = 3, ExemptFighters = 1 << Player2 }));
        var counter = new AdvanceCountingStage();
        Run(new TickStages { Movement = counter }, frames: 6, superFlash);

        Assert.Equal(
            [(true, true, false), (false, true, true), (false, true, true), (false, true, true), (true, true, false),
                (true, true, false)],
            counter.Frames);
    }

    [Fact]
    public void AFreezeRequestedAtTheEndOfAFrameStartsOnTheNextFrame()
    {
        var module = new ScriptedModule(1)
        {
            OnEndOfFrame = context =>
            {
                if (context.State.Frame == 0)
                {
                    context.RequestFreeze(new FreezeRequest { Frames = 1 });
                }
            },
        };
        var counter = new AdvanceCountingStage();
        Run(new TickStages { Movement = counter }, frames: 3, module);

        Assert.Equal([false, true, false], counter.Frames.ConvertAll(frame => frame.IsFrozen));
    }

    [Fact]
    public void HitstopOverrideMakesTheFighterSkipExactlyThatManyFrames()
    {
        var module = new ScriptedModule(1)
        {
            OnEndOfFrame = context =>
            {
                if (context.State.Frame == 0)
                {
                    context.RequestHitstopOverride(Player1, new HitstopOverrideRequest { Frames = 4 });
                }
            },
        };
        var counter = new AdvanceCountingStage();
        Run(new TickStages { Movement = counter }, frames: 7, module);

        Assert.Equal(
            [true, false, false, false, false, true, true], counter.Frames.ConvertAll(frame => frame.Player1));
        Assert.Equal(7, counter.AdvancedFrames(Player2));
    }

    [Fact]
    public void HitstopOverrideFromTheLaterModuleSlotWins()
    {
        var first = OnFrame(0, context => context.RequestHitstopOverride(
            Player2, new HitstopOverrideRequest { Frames = 9 }), id: 1);
        var second = OnFrame(0, context => context.RequestHitstopOverride(
            Player2, new HitstopOverrideRequest { Frames = 2 }), id: 2);
        var counter = new AdvanceCountingStage();
        Run(new TickStages { Movement = counter }, frames: 4, first, second);

        Assert.Equal([false, false, true, true], counter.Frames.ConvertAll(frame => frame.Player2));
    }

    [Fact]
    public void ACancelRequestReachesTheStateMachineFromAnyStateInTheSameFrame()
    {
        var module = OnFrame(0, context => context.RequestCancel(
            Player1, new CancelRequest { TargetMove = 7, Priority = 1 }));
        var stateMachine = new CancelReadingStage();
        Run(new TickStages { StateMachines = stateMachine }, frames: 1, module);

        Assert.Equal(7, stateMachine.SeenTargetMove[Player1]);
        Assert.Equal(-1, stateMachine.SeenTargetMove[Player2]);
    }

    [Fact]
    public void TheHighestPriorityCancelWinsAndTiesGoToTheEarlierSlot()
    {
        var low = OnFrame(0, context => context.RequestCancel(
            Player1, new CancelRequest { TargetMove = 1, Priority = 1 }), id: 1);
        var high = OnFrame(0, context => context.RequestCancel(
            Player1, new CancelRequest { TargetMove = 2, Priority = 5 }), id: 2);
        var tie = OnFrame(0, context => context.RequestCancel(
            Player1, new CancelRequest { TargetMove = 3, Priority = 5 }), id: 3);
        var stateMachine = new CancelReadingStage();
        Run(new TickStages { StateMachines = stateMachine }, frames: 1, low, high, tie);

        Assert.Equal(2, stateMachine.SeenTargetMove[Player1]);
    }

    [Fact]
    public void AnUnconsumedCancelStaysPendingUntilTheStateMachineTakesIt()
    {
        var module = OnFrame(0, context => context.RequestCancel(
            Player1, new CancelRequest { TargetMove = 4, Priority = 1 }));
        MatchState state = Run(new TickStages(), frames: 3, module);

        Assert.Equal(new PendingCancel { IsPending = 1, TargetMove = 4, Priority = 1 }, state.Fighters[Player1].Cancel);
    }

    [Fact]
    public void StunOverrideReplacesTheFightersStunFromTheLaterSlot()
    {
        var first = OnFrame(0, context => context.RequestStunOverride(
            Player2, new StunOverrideRequest { Frames = 9 }), id: 1);
        var second = OnFrame(0, context => context.RequestStunOverride(
            Player2, new StunOverrideRequest { Frames = 4 }), id: 2);
        MatchState state = Run(new TickStages(), frames: 1, first, second);

        Assert.Equal(4, state.Fighters[Player2].StunFramesLeft);
        Assert.Equal(0, state.Fighters[Player1].StunFramesLeft);
    }

    [Fact]
    public void AModuleEventIsEmittedInTheFrameItWasRequestedWithTheModuleAndCodePacked()
    {
        var module = OnFrame(0, context => context.RequestEvent(
            Player2,
            new ModuleEventRequest { Module = new ModuleId(7), Code = 3, Position = new IntVector2(10, 20) }));
        MatchState state = Run(new TickStages(), frames: 1, module);

        FrameEvent frameEvent = Assert.Single(state.Events.OfFrame(0).ToArray());
        Assert.Equal(FrameEventKind.Module, frameEvent.Kind);
        Assert.Equal(Player2, frameEvent.Fighter);
        Assert.Equal(new IntVector2(10, 20), frameEvent.Position);
        Assert.Equal(new ModuleId(7), ModuleEventRequest.ModuleOf(frameEvent.Detail));
        Assert.Equal(3, ModuleEventRequest.CodeOf(frameEvent.Detail));
    }

    [Fact]
    public void RequestsAreClearedAfterTheyAreApplied()
    {
        int calls = 0;
        var module = OnFrame(0, context =>
        {
            calls++;
            context.RequestHitstopOverride(Player1, new HitstopOverrideRequest { Frames = 1 });
        });
        var counter = new AdvanceCountingStage();
        Run(new TickStages { Movement = counter }, frames: 4, module);

        Assert.Equal(1, calls);
        Assert.Equal([false, true, true, true], counter.Frames.ConvertAll(frame => frame.Player1));
    }

    private static ScriptedModule OnFrame(int frame, ScriptedModule.HookAction action, int id = 1) => new(id)
    {
        OnAfterInput = context =>
        {
            if (context.State.Frame == frame)
            {
                action(context);
            }
        },
    };

    private static MatchState Run(TickStages stages, int frames, params IMechanicModule[] modules)
    {
        TickPipeline pipeline = Pipelines.Create(stages, modules);
        MatchState state = default;
        for (int frame = 0; frame < frames; frame++)
        {
            pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        }

        return state;
    }

    // Stands in for the move state machine (P1-08): takes each fighter's pending cancel, whatever state it is in.
    private sealed class CancelReadingStage : ITickStage
    {
        public int[] SeenTargetMove { get; } = [-1, -1];

        public void Run(ref MatchState state, in FrameActivity activity)
        {
            for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
            {
                ref PendingCancel cancel = ref state.Fighters[fighter].Cancel;
                if (cancel.IsPending == 1)
                {
                    SeenTargetMove[fighter] = cancel.TargetMove;
                    cancel = default;
                }
            }
        }
    }
}
