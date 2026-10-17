using System;
using Sim.Characters;
using Sim.Fighters.Movement;
using Sim.Fighters.States;
using Sim.Input;
using Sim.State;
using Xunit;
using static Sim.Tests.Fighters.States.TestCharacter;

namespace Sim.Tests.Fighters.States;

public sealed class FighterStateMachineTests
{
    private const ushort Forward = InputWord.Right;
    private const ushort Back = InputWord.Left;

    private static readonly CharacterData _character = Create();

    [Fact]
    public void FighterActionMirrorsBasicStateByNameAndValue()
    {
        foreach (BasicState state in Enum.GetValues<BasicState>())
        {
            Assert.Equal(state.ToString(), ((FighterAction)state).ToString());
        }
    }

    [Theory]
    [InlineData(Forward, FighterAction.WalkForward)]
    [InlineData(Back, FighterAction.WalkBack)]
    [InlineData(InputWord.Down, FighterAction.Crouch)]
    [InlineData(InputWord.Down | Back, FighterAction.Crouch)]
    [InlineData(InputWord.Up, FighterAction.Prejump)]
    [InlineData(0, FighterAction.Idle)]
    public void AFreeFighterFollowsTheHeldDirection(ushort input, FighterAction expected)
    {
        var driver = new FighterDriver(_character);

        Assert.Equal(expected, driver.Frame(input).Action);
    }

    [Fact]
    public void DirectionsAreRelativeToFacing()
    {
        var facingLeft = new FighterDriver(_character, facing: -1);

        Assert.Equal(FighterAction.WalkForward, facingLeft.Frame(InputWord.Left).Action);
        Assert.Equal(FighterAction.WalkBack, facingLeft.Frame(InputWord.Right).Action);
    }

    [Fact]
    public void PrejumpLastsTheCharactersPrejumpFramesThenJumps()
    {
        var driver = new FighterDriver(_character);

        driver.Frame(InputWord.Up);
        driver.Frames(PrejumpFrames - 1, InputWord.Up);
        Assert.Equal(FighterAction.Prejump, driver.Action);
        Assert.Equal(FighterAction.Jump, driver.Frame(InputWord.Up).Action);
    }

    [Fact]
    public void PressingAButtonStartsTheMoveOnThatFrameAndItRunsItsFrameData()
    {
        var driver = new FighterDriver(_character);

        driver.Frame(ButtonA);
        Assert.Equal(
            (FighterAction.Attack, Normal5A, 1), (driver.Action, driver.Fighter.Move, driver.Fighter.MoveFrame));
        Assert.Equal(MovePhase.Startup, driver.Phase);
        driver.Frames(3);
        Assert.Equal(MovePhase.Startup, driver.Phase);
        driver.Frame();
        Assert.Equal((5, MovePhase.Active), (driver.Fighter.MoveFrame, driver.Phase));
        driver.Frames(2);
        Assert.Equal(MovePhase.Active, driver.Phase);
        driver.Frame();
        Assert.Equal(MovePhase.Recovery, driver.Phase);
        driver.Frames(7);
        Assert.Equal((FighterAction.Attack, 15), (driver.Action, driver.Fighter.MoveFrame));
        Assert.Equal(FighterAction.Idle, driver.Frame().Action);
    }

    [Fact]
    public void TheFirstFreeFrameAfterAMoveCanStartTheNextMove()
    {
        var driver = new FighterDriver(_character);

        driver.Frame(ButtonA).Frames(14).Frame(InputWord.Down | ButtonB);

        Assert.Equal(
            (FighterAction.Attack, Normal2B, 1), (driver.Action, driver.Fighter.Move, driver.Fighter.MoveFrame));
    }

    [Fact]
    public void HoldingAButtonDoesNotStartTheMoveAgain()
    {
        var driver = new FighterDriver(_character);

        driver.Frames(16, ButtonA);
        driver.Frame(ButtonA);

        Assert.Equal(FighterAction.Idle, driver.Action);
    }

    [Fact]
    public void AMoveEndsIntoTheHeldDirection()
    {
        var driver = new FighterDriver(_character);

        driver.Frame(ButtonA).Frames(15, InputWord.Down);

        Assert.Equal(FighterAction.Crouch, driver.Action);
    }

    [Fact]
    public void ACrouchingMotionNeedsDownHeld()
    {
        var standing = new FighterDriver(_character).Frame(ButtonB);
        var crouching = new FighterDriver(_character).Frame(InputWord.Down | ButtonB);

        Assert.Equal(FighterAction.Idle, standing.Action);
        Assert.Equal(Normal2B, crouching.Fighter.Move);
    }

    [Fact]
    public void AHitOpensTheCancelWindowIntoSpecials()
    {
        var driver = new FighterDriver(_character);
        driver.Frame(ButtonA).Frames(5);
        driver.Fighter.MoveOutcome |= CancelOn.Hit;

        driver.Frame(Forward | ButtonC);

        Assert.Equal((Special6C, 1), (driver.Fighter.Move, driver.Fighter.MoveFrame));
    }

    [Fact]
    public void WithoutContactTheHitCancelWindowStaysClosed()
    {
        var driver = new FighterDriver(_character);

        driver.Frame(ButtonA).Frames(5).Frame(Forward | ButtonC);

        Assert.Equal(Normal5A, driver.Fighter.Move);
    }

    [Fact]
    public void TheHitCancelWindowClosesAfterItsLastFrame()
    {
        var driver = new FighterDriver(_character);
        driver.Frame(ButtonA).Frames(11);
        driver.Fighter.MoveOutcome |= CancelOn.Block;

        driver.Frame(Forward | ButtonC);

        Assert.Equal((Normal5A, 13), (driver.Fighter.Move, driver.Fighter.MoveFrame));
    }

    [Fact]
    public void AWhiffMarksTheMoveAfterItsActiveFramesAndOpensTheWhiffWindow()
    {
        var driver = new FighterDriver(_character);
        driver.Frame(ButtonA).Frames(6);
        Assert.Equal(CancelOn.None, driver.Fighter.MoveOutcome);

        driver.Frame();
        Assert.Equal(CancelOn.Whiff, driver.Fighter.MoveOutcome);
        driver.Frame(ButtonA);
        Assert.Equal((Normal5A, 1), (driver.Fighter.Move, driver.Fighter.MoveFrame));
    }

    [Fact]
    public void AClashedMoveIsNotMarkedAsAWhiff()
    {
        var driver = new FighterDriver(_character);
        driver.Frame(ButtonA).Frames(5);
        driver.Fighter.MoveOutcome |= CancelOn.Clash;

        driver.Frame();

        Assert.Equal(CancelOn.Clash, driver.Fighter.MoveOutcome);
    }

    [Fact]
    public void APendingCancelIsTakenFromAnyAction()
    {
        var driver = new FighterDriver(_character);
        MoveEntry.EnterStun(ref driver.Fighter, FighterAction.Blockstun, 10, _character);
        driver.Fighter.Cancel = new PendingCancel { IsPending = 1, TargetMove = Special6C, Priority = 1 };

        driver.Frame();

        Assert.Equal(
            (FighterAction.Attack, Special6C, 1), (driver.Action, driver.Fighter.Move, driver.Fighter.MoveFrame));
        Assert.Equal(default, driver.Fighter.Cancel);
        Assert.Equal(0, driver.Fighter.StunFramesLeft);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(4)]
    public void APendingCancelIntoAMoveThatIsNotAnAttackFailsLoudly(int target)
    {
        var driver = new FighterDriver(_character);
        driver.Fighter.Cancel = new PendingCancel { IsPending = 1, TargetMove = target };

        Assert.Throws<InvalidOperationException>(() => driver.Frame());
    }

    [Fact]
    public void StunLastsItsFramesThenEndsIntoTheHeldDirection()
    {
        var driver = new FighterDriver(_character);
        MoveEntry.EnterStun(ref driver.Fighter, FighterAction.Hitstun, 4, _character);

        driver.Frames(4, InputWord.Down);
        Assert.Equal((FighterAction.Hitstun, 0), (driver.Action, driver.Fighter.StunFramesLeft));
        Assert.Equal(FighterAction.Crouch, driver.Frame(InputWord.Down).Action);
    }

    [Fact]
    public void InTheAirOnlyAirMovesStartAndLandingEndsThem()
    {
        var driver = new FighterDriver(_character);
        MoveEntry.EnterBasic(ref driver.Fighter, FighterAction.Jump, _character);
        driver.Fighter.IsAirborne = 1;

        driver.Frame(ButtonA);
        Assert.Equal(FighterAction.Jump, driver.Action);
        driver.Frame(ButtonB);
        Assert.Equal(AirB, driver.Fighter.Move);

        driver.Fighter.IsAirborne = 0;
        Assert.Equal(FighterAction.Landing, driver.Frame().Action);
        driver.Frames(LandingFrames);
        Assert.Equal(FighterAction.Idle, driver.Action);
    }

    [Fact]
    public void AJumpLandsOnceMovementClearsTheAirborneFlag()
    {
        var driver = new FighterDriver(_character);
        MoveEntry.EnterBasic(ref driver.Fighter, FighterAction.Jump, _character);
        driver.Fighter.IsAirborne = 1;
        driver.Frames(10);
        Assert.Equal(FighterAction.Jump, driver.Action);

        driver.Fighter.IsAirborne = 0;

        Assert.Equal(FighterAction.Landing, driver.Frame().Action);
    }

    [Fact]
    public void KnockdownLeadsToWakeUpThenNeutral()
    {
        var driver = new FighterDriver(_character);
        MoveEntry.EnterBasic(ref driver.Fighter, FighterAction.Knockdown, _character);

        driver.Frames(KnockdownFrames);
        Assert.Equal(FighterAction.WakeUp, driver.Action);
        driver.Frames(WakeUpFrames, InputWord.Down);
        Assert.Equal(FighterAction.Crouch, driver.Action);
    }

    [Fact]
    public void AKnockedOutFighterStaysDownOnTheKnockdownsLastFrame()
    {
        var driver = new FighterDriver(_character);
        MoveEntry.EnterBasic(ref driver.Fighter, FighterAction.Knockdown, _character);
        driver.Fighter.Health = 0;

        driver.Frames(KnockdownFrames * 3, InputWord.Up);

        Assert.Equal((FighterAction.Knockdown, KnockdownFrames), (driver.Action, driver.Fighter.MoveFrame));
    }

    [Fact]
    public void AirHitstunEndsInKnockdownOnLanding()
    {
        var driver = new FighterDriver(_character);
        MoveEntry.EnterStun(ref driver.Fighter, FighterAction.AirHitstun, 3, _character);
        driver.Fighter.IsAirborne = 1;
        driver.Frames(8);
        Assert.Equal(FighterAction.AirHitstun, driver.Action);

        driver.Fighter.IsAirborne = 0;

        Assert.Equal(FighterAction.Knockdown, driver.Frame().Action);
    }

    // The jump move loops; landing on the frame it loops back to 1 must still land.
    [Fact]
    public void AJumpLandsIntoLandingOnTheFrameItsMoveLoops()
    {
        var driver = new FighterDriver(_character);
        MoveEntry.EnterBasic(ref driver.Fighter, FighterAction.Jump, _character);
        driver.Fighter.IsAirborne = 1;
        driver.Frames(_character.Moves[driver.Fighter.Move].TotalFrames - 1);

        driver.Fighter.IsAirborne = 0;

        Assert.Equal(FighterAction.Landing, driver.Frame().Action);
    }

    [Fact]
    public void AnAirDashLandingEarlyGoesToLanding()
    {
        var driver = new FighterDriver(_character);
        MoveEntry.EnterBasic(ref driver.Fighter, FighterAction.AirDash, _character);
        driver.Fighter.IsAirborne = 1;
        driver.Frame();
        Assert.Equal(FighterAction.AirDash, driver.Action);

        driver.Fighter.IsAirborne = 0;

        Assert.Equal(FighterAction.Landing, driver.Frame().Action);
    }

    [Fact]
    public void TheTakeoffRequestLastsOnlyTheFrameTheJumpStarts()
    {
        var driver = new FighterDriver(_character);

        driver.Frames(PrejumpFrames + 1, InputWord.Up | Forward);
        Assert.Equal((Takeoff.Jump, 1), (driver.Fighter.Takeoff, driver.Fighter.TakeoffDirection));
        driver.Fighter.IsAirborne = 1;

        Assert.Equal(Takeoff.None, driver.Frame().Fighter.Takeoff);
    }

    [Fact]
    public void ARunContinuesWhileForwardIsHeldAndStopsWhenReleased()
    {
        var driver = new FighterDriver(_character, new DashOnDemand());

        driver.Frame(DashOnDemand.Dash | Forward).Frames(DashFrames * 3, Forward);
        Assert.Equal(FighterAction.Dash, driver.Action);
        Assert.Equal(FighterAction.Idle, driver.Frame().Action);
    }

    [Fact]
    public void AStepDashEndsAfterItsMoveEvenWithForwardHeld()
    {
        var driver = new FighterDriver(Create(DashStyle.Step), new DashOnDemand());

        driver.Frame(DashOnDemand.Dash).Frames(DashFrames - 1, Forward);
        Assert.Equal(FighterAction.Dash, driver.Action);

        Assert.Equal(FighterAction.WalkForward, driver.Frame(Forward).Action);
    }

    [Fact]
    public void ABackdashEndsAfterItsMove()
    {
        var driver = new FighterDriver(_character, new DashOnDemand());

        driver.Frame(DashOnDemand.Backdash);
        Assert.Equal(FighterAction.Backdash, driver.Action);
        driver.Frames(DashFrames - 1);
        Assert.Equal(FighterAction.Backdash, driver.Action);
        Assert.Equal(FighterAction.Idle, driver.Frame().Action);
    }

    [Fact]
    public void AnUndefinedBasicStatePlaysTheIdleMove()
    {
        CharacterData character = Create(without: BasicState.WalkForward);
        var driver = new FighterDriver(character);

        driver.Frame(Forward);

        Assert.Equal(FighterAction.WalkForward, driver.Action);
        Assert.Equal(character.BasicStateMove(BasicState.Idle), driver.Fighter.Move);
    }

    [Fact]
    public void AdvancingAllocatesNothing()
    {
        var driver = new FighterDriver(_character);
        driver.Frames(20, ButtonA);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < 200; frame++)
        {
            driver.Frame((ushort)((frame % 7 == 0 ? ButtonA : 0) | (frame & InputWord.Down)));
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    // Triggers a dash on one spare input bit and a backdash on another, standing in for the dash input (P1-06/P1-09).
    private sealed class DashOnDemand : IMoveInput
    {
        public const ushort Dash = 1 << 14;
        public const ushort Backdash = 1 << 15;

        private readonly ButtonPressMoveInput _moves = new();

        public bool IsTriggered(in InputHistory history, in MoveData move, int facing) =>
            _moves.IsTriggered(in history, in move, facing);

        public bool IsShortcutTriggered(in InputHistory history, in MoveData move, int facing) => false;

        public bool IsDashTriggered(in InputHistory history, int facing) => history.FramesAgo(0).IsHeld(Dash);

        public bool IsBackdashTriggered(in InputHistory history, int facing) => history.FramesAgo(0).IsHeld(Backdash);

        public bool IsHighJumpTriggered(in InputHistory history, int facing) =>
            _moves.IsHighJumpTriggered(in history, facing);

        public bool IsAirJumpTriggered(in InputHistory history, int facing) =>
            _moves.IsAirJumpTriggered(in history, facing);
    }
}
