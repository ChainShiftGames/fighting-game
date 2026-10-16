using System;
using Sim.Characters;
using Sim.Events;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Math;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;
using Sim.Tests.Modules;
using Xunit;
using static Sim.Tests.Throws.ThrowCharacter;

namespace Sim.Tests.Throws;

public sealed class ThrowStagesTests
{
    private const int Frame = 40;
    private static readonly FrameActivity _onlyPlayer1Advances = new(advancingFighters: 1, isFrozen: false);

    private readonly ThrowStages _stages = new();

    [Fact]
    public void AThrowInRangeGrabsAndHoldsTheDefender()
    {
        MatchState state = P1Throws(At(30_000, -1));

        _stages.Run(ref state);

        FighterState defender = state.Fighters[1];
        Assert.Equal(
            (FighterAction.Thrown, ThrowStages.TechWindowFrames, Throw),
            (defender.Action, defender.ThrowTechFramesLeft, defender.ThrownByMove));
        Assert.Equal((Health, 0), (defender.Health, defender.Timing.HitstopFramesLeft));
        Assert.Equal(CancelOn.Hit, state.Fighters[0].MoveOutcome);
        FrameEvent grab = Assert.Single(state.Events.OfFrame(Frame).ToArray());
        Assert.Equal((FrameEventKind.ThrowGrab, 0, Throw), (grab.Kind, grab.Fighter, grab.Detail));
    }

    [Fact]
    public void AThrowOutOfRangeDoesNothing()
    {
        MatchState state = P1Throws(At(70_000, -1));
        MatchState before = state;

        _stages.Run(ref state);

        Assert.Equal(StateChecksum.Compute(in before), StateChecksum.Compute(in state));
    }

    [Fact]
    public void TheThrowLandsWhenTheTechWindowCloses()
    {
        MatchState state = Grabbed();

        for (int frame = 1; frame < ThrowStages.TechWindowFrames; frame++)
        {
            _stages.RunHolds(ref state, FrameActivity.Everyone);
            Assert.Equal(FighterAction.Thrown, state.Fighters[1].Action);
        }

        _stages.RunHolds(ref state, FrameActivity.Everyone);

        FighterState defender = state.Fighters[1];
        Assert.Equal((Health - ThrowHit.Damage, FighterAction.Knockdown), (defender.Health, defender.Action));
        Assert.Equal(new IntVector2(-30_000 + ReleaseX, 0), defender.Position);
        Assert.Equal((0, 0), (defender.ThrowTechFramesLeft, defender.ThrownByMove));
        Assert.Equal(ThrowHit.Hitstop, defender.Timing.HitstopFramesLeft);
        Assert.Equal(ThrowHit.Hitstop, state.Fighters[0].Timing.HitstopFramesLeft);
        CountingState hooks = ThrowStages.Hooks(ref state);
        Assert.Equal((1, 0, 0, 1), (hooks.OnThrow, hooks.OnHit, hooks.LastAttacker, hooks.LastDefender));
    }

    [Fact]
    public void ATechInputInTheWindowSeparatesBothFighters()
    {
        MatchState state = Grabbed();
        state.Inputs[1].Record(new InputWord(ThrowButtons));

        _stages.RunHolds(ref state, FrameActivity.Everyone);

        Assert.Equal(FighterAction.ThrowTech, state.Fighters[0].Action);
        Assert.Equal(FighterAction.ThrowTech, state.Fighters[1].Action);
        Assert.Equal((Health, 0), (state.Fighters[1].Health, state.Fighters[1].ThrowTechFramesLeft));
        Assert.Equal(0, ThrowStages.Hooks(ref state).OnThrow);
    }

    [Fact]
    public void ATechPressOlderThanTheButtonBufferDoesNotCount()
    {
        MatchState state = Grabbed();
        state.Inputs[1].Record(new InputWord(ThrowButtons));
        for (int frame = 0; frame < ThrowStages.ButtonBuffer + 1; frame++)
        {
            state.Inputs[1].Record(new InputWord(ThrowButtons));
        }

        _stages.RunHolds(ref state, FrameActivity.Everyone);

        Assert.Equal(FighterAction.Thrown, state.Fighters[1].Action);
    }

    [Fact]
    public void AStunnedThrowerLetsGoOfTheHold()
    {
        MatchState state = Grabbed();
        state.Fighters[0].Action = FighterAction.Hitstun;

        _stages.RunHolds(ref state, FrameActivity.Everyone);

        FighterState defender = state.Fighters[1];
        Assert.Equal((FighterAction.Idle, Health, 0), (defender.Action, defender.Health, defender.ThrowTechFramesLeft));
    }

    [Fact]
    public void HitstopPausesTheTechWindow()
    {
        MatchState state = Grabbed();

        _stages.RunHolds(ref state, in _onlyPlayer1Advances);

        Assert.Equal(ThrowStages.TechWindowFrames, state.Fighters[1].ThrowTechFramesLeft);
    }

    [Fact]
    public void AFighterThatLeftTheHoldSomeOtherWayForgetsIt()
    {
        MatchState state = Grabbed();
        state.Fighters[1].Action = FighterAction.Hitstun;

        _stages.RunHolds(ref state, FrameActivity.Everyone);

        Assert.Equal((FighterAction.Hitstun, 0, 0), (
            state.Fighters[1].Action, state.Fighters[1].ThrowTechFramesLeft, state.Fighters[1].ThrownByMove));
    }

    [Fact]
    public void ThrowsGrabbingOnTheSameFrameTech()
    {
        MatchState state = BothThrow();

        _stages.Run(ref state);

        Assert.Equal(FighterAction.ThrowTech, state.Fighters[0].Action);
        Assert.Equal(FighterAction.ThrowTech, state.Fighters[1].Action);
        Assert.Equal(
            [(FrameEventKind.ThrowTech, 0), (FrameEventKind.ThrowTech, 1)],
            Array.ConvertAll(
                state.Events.OfFrame(Frame).ToArray(), frameEvent => (frameEvent.Kind, frameEvent.Fighter)));
    }

    [Fact]
    public void UnderTheThrowRuleTheGrabDropsTheDefendersStrike()
    {
        MatchState state = P1Throws(At(30_000, -1));
        _stages.Strikes.Record(1, IntVector2.Zero);

        _stages.Run(ref state);

        Assert.Equal(FighterAction.Thrown, state.Fighters[1].Action);
        Assert.False(_stages.Strikes.Touches(1));
    }

    [Fact]
    public void UnderTheStrikeRuleTheDefendersStrikeStopsTheGrab()
    {
        ThrowStages stages = new(ThrowPriority.Strike);
        MatchState state = P1Throws(At(30_000, -1));
        stages.Strikes.Record(1, IntVector2.Zero);

        stages.Run(ref state);

        Assert.Equal(FighterAction.Idle, state.Fighters[1].Action);
        Assert.Equal(CancelOn.None, state.Fighters[0].MoveOutcome);
        Assert.True(stages.Strikes.Touches(1));
    }

    [Fact]
    public void SwappingAndMirroringThePlayersMirrorsTheGrab()
    {
        MatchState original = P1Throws(At(30_000, -1));
        MatchState swapped = default;
        swapped.Frame = Frame;
        swapped.Fighters[0] = Mirror(original.Fighters[1]);
        swapped.Fighters[1] = Mirror(original.Fighters[0]);

        _stages.Run(ref original);
        _stages.Run(ref swapped);

        Assert.Equal(Mirror(original.Fighters[0]), swapped.Fighters[1]);
        Assert.Equal(Mirror(original.Fighters[1]), swapped.Fighters[0]);
    }

    [Fact]
    public void GrabsHoldsAndTechsAllocateNothing()
    {
        MatchState grab = P1Throws(At(30_000, -1));
        MatchState held = Grabbed();
        MatchState clash = BothThrow();
        RunAll(grab, held, clash);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            RunAll(grab, held, clash);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private void RunAll(MatchState grab, MatchState held, MatchState clash)
    {
        _stages.Run(ref grab);
        _stages.RunHolds(ref held, FrameActivity.Everyone);
        _stages.Run(ref clash);
    }

    // P1 at -30000 has just grabbed P2 at 30000; the next hold frame is the first of the tech window.
    private MatchState Grabbed()
    {
        MatchState state = P1Throws(At(30_000, -1));
        _stages.Run(ref state);
        state.Fighters[0].MoveFrame = ActiveFrame + 2;
        return state;
    }

    private static MatchState P1Throws(FighterState defender)
    {
        MatchState state = default;
        state.Frame = Frame;
        state.Fighters[0] = Throwing(-30_000, 1);
        state.Fighters[1] = defender;
        return state;
    }

    private static MatchState BothThrow()
    {
        MatchState state = P1Throws(Throwing(30_000, -1));
        return state;
    }

    private static FighterState Mirror(FighterState fighter) => fighter with
    {
        Position = new IntVector2(-fighter.Position.X, fighter.Position.Y),
        Velocity = new IntVector2(-fighter.Velocity.X, fighter.Velocity.Y),
        Facing = -fighter.Facing,
    };
}
