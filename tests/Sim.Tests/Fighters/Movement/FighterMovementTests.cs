using System;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.Input;
using Xunit;
using static Sim.Tests.Fighters.States.TestCharacter;

namespace Sim.Tests.Fighters.Movement;

public sealed class FighterMovementTests
{
    private const ushort Forward = InputWord.Right;
    private const ushort Back = InputWord.Left;

    // With jump velocity 20 000 and gravity 1 000 the height after n airborne frames is 20 000n - 500n(n - 1):
    // highest after 20 and 21 frames, back on the ground on the 41st.
    private const int JumpPeak = 210_000;
    private const int JumpAirborneFrames = 41;

    private static readonly CharacterData _character = Create();

    [Fact]
    public void WalkingMovesByTheWalkSpeedsRelativeToFacing()
    {
        var right = new MovingFighterDriver(_character);
        var left = new MovingFighterDriver(_character, facing: -1);

        right.Frames(3, Forward);
        left.Frames(3, InputWord.Left);
        Assert.Equal((3 * WalkForwardSpeed, -3 * WalkForwardSpeed), (right.X, left.X));

        right.Frames(2, Back);
        Assert.Equal((3 * WalkForwardSpeed) - (2 * WalkBackSpeed), right.X);
    }

    [Fact]
    public void StandingCrouchingAndAttackingStayInPlace()
    {
        var driver = new MovingFighterDriver(_character);

        driver.Frames(3, Forward).Frame().Frames(3, InputWord.Down).Frame(ButtonA).Frames(10);

        Assert.Equal(3 * WalkForwardSpeed, driver.X);
        Assert.Equal(0, driver.Fighter.Velocity.X);
    }

    [Fact]
    public void ANeutralJumpStaysGroundedThroughPrejumpThenFollowsGravityAndLands()
    {
        var driver = new MovingFighterDriver(_character);

        driver.Frames(PrejumpFrames, InputWord.Up);
        Assert.Equal((FighterAction.Prejump, 0, 0), (driver.Action, driver.Y, driver.Fighter.IsAirborne));

        driver.Frame(InputWord.Up);
        Assert.Equal((FighterAction.Jump, JumpVelocity, 1), (driver.Action, driver.Y, driver.Fighter.IsAirborne));
        Assert.Equal(JumpVelocity - Gravity, driver.Fighter.Velocity.Y);

        driver.Frames(19);
        Assert.Equal(JumpPeak, driver.Y);
        Assert.Equal(JumpAirborneFrames - 20, driver.FramesUntilGrounded());
        Assert.Equal((0, 0), (driver.X, driver.Y));
        Assert.Equal(FighterAction.Landing, driver.Frame().Action);
    }

    [Theory]
    [InlineData(Forward, 1, JumpForwardSpeed)]
    [InlineData(Back, 1, -JumpBackSpeed)]
    [InlineData(InputWord.Left, -1, -JumpForwardSpeed)]
    [InlineData(0, 1, 0)]
    public void JumpArcsUseTheDirectionHeldAtTakeoff(ushort direction, int facing, int speed)
    {
        var driver = new MovingFighterDriver(_character, facing);

        // The prejump starts straight up; only the direction on the takeoff frame counts.
        driver.Frames(PrejumpFrames, InputWord.Up).Frame((ushort)(InputWord.Up | direction));
        driver.FramesUntilGrounded();

        Assert.Equal(speed * JumpAirborneFrames, driver.X);
    }

    [Fact]
    public void DownThenUpStartsAHighJump()
    {
        var driver = new MovingFighterDriver(_character);

        driver.Frame(InputWord.Down).Frames(PrejumpFrames + 1, InputWord.Up);

        Assert.Equal(HighJumpVelocity, driver.Y);
        Assert.Equal(0, driver.Fighter.IsHighJump);
    }

    [Fact]
    public void ARunMovesAtTheDashMovesVelocityUntilForwardIsReleased()
    {
        var driver = new MovingFighterDriver(_character);

        driver.Frame((ushort)(MovingFighterDriver.DashButton | Forward)).Frames((DashFrames * 2) - 1, Forward);
        Assert.Equal((FighterAction.Dash, DashFrames * 2 * DashSpeed), (driver.Action, driver.X));

        driver.Frame();
        Assert.Equal((FighterAction.Idle, DashFrames * 2 * DashSpeed), (driver.Action, driver.X));
    }

    [Fact]
    public void ABackdashTravelsForItsVelocityFramesThenStopsAndIsInvulnerableEarly()
    {
        var driver = new MovingFighterDriver(_character);

        driver.Frame((ushort)(MovingFighterDriver.DashButton | Back));
        Assert.Equal(FighterAction.Backdash, driver.Action);
        Assert.True(IsInvulnerable(driver));
        driver.Frames(BackdashInvulnerableFrames - 1);
        Assert.True(IsInvulnerable(driver));
        driver.Frame();
        Assert.False(IsInvulnerable(driver));

        driver.Frames(DashFrames - BackdashInvulnerableFrames - 1);
        Assert.Equal((FighterAction.Backdash, -BackdashSpeed * BackdashMovingFrames), (driver.Action, driver.X));
    }

    [Fact]
    public void ADoubleJumpRestartsTheJumpAndSpendsAnAirAction()
    {
        var driver = new MovingFighterDriver(_character);
        driver.JumpTakeoff().Frames(10);
        int height = driver.Y;

        driver.Frame(InputWord.Up);

        Assert.Equal((FighterAction.Jump, 1), (driver.Action, driver.Fighter.AirActionsUsed));
        Assert.Equal(height + JumpVelocity, driver.Y);
    }

    [Fact]
    public void HoldingUpFromTheFirstJumpIsNoDoubleJump()
    {
        var driver = new MovingFighterDriver(_character);

        driver.JumpTakeoff().Frames(10, InputWord.Up);

        Assert.Equal(0, driver.Fighter.AirActionsUsed);
    }

    [Fact]
    public void AnAirDashFliesLevelThenFallsWithItsMomentum()
    {
        var driver = new MovingFighterDriver(_character);
        driver.JumpTakeoff().Frames(5);
        (int x, int y) = (driver.X, driver.Y);

        driver.Frame(MovingFighterDriver.DashButton).Frames(AirDashFrames - 1);
        Assert.Equal(
            (FighterAction.AirDash, x + (AirDashFrames * AirDashSpeed), y), (driver.Action, driver.X, driver.Y));

        driver.Frame();
        Assert.Equal(FighterAction.Jump, driver.Action);
        Assert.Equal((x + ((AirDashFrames + 1) * AirDashSpeed), y), (driver.X, driver.Y));
        Assert.Equal(-Gravity, driver.Fighter.Velocity.Y);
    }

    [Fact]
    public void ABackwardAirDashUsesItsOwnMoveVelocity()
    {
        var driver = new MovingFighterDriver(_character, facing: -1);
        driver.JumpTakeoff().Frame();

        driver.Frame((ushort)(MovingFighterDriver.DashButton | InputWord.Right));

        Assert.Equal((FighterAction.AirBackdash, AirBackdashSpeed), (driver.Action, driver.Fighter.Velocity.X));
    }

    [Fact]
    public void AirActionsShareOneBudgetThatLandingRefills()
    {
        var driver = new MovingFighterDriver(_character);
        driver.JumpTakeoff().Frames(2);

        driver.Frame(InputWord.Up).Frame().Frame(MovingFighterDriver.DashButton).Frames(AirDashFrames);
        Assert.Equal((AirActions, FighterAction.Jump), (driver.Fighter.AirActionsUsed, driver.Action));

        // Budget spent: the fresh up press is no third air action, the fall just continues.
        driver.Frame(InputWord.Up);
        Assert.Equal((AirActions, -2 * Gravity), (driver.Fighter.AirActionsUsed, driver.Fighter.Velocity.Y));

        driver.FramesUntilGrounded();
        Assert.Equal(0, driver.Fighter.AirActionsUsed);
    }

    [Fact]
    public void AnAirAttackKeepsTheJumpMomentumAndLandsIntoLanding()
    {
        var driver = new MovingFighterDriver(_character);
        driver.JumpTakeoff(Forward).Frames(30);

        driver.Frame(ButtonB);
        Assert.Equal((FighterAction.Attack, AirB), (driver.Action, driver.Fighter.Move));
        driver.FramesUntilGrounded();

        Assert.Equal(JumpForwardSpeed * JumpAirborneFrames, driver.X);
        Assert.Equal(FighterAction.Landing, driver.Frame().Action);
    }

    [Fact]
    public void MovingAllocatesNothing()
    {
        var driver = new MovingFighterDriver(_character);
        driver.JumpTakeoff(Forward).Frames(5);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < 200; frame++)
        {
            driver.Frame((ushort)(frame % 9 == 0 ? InputWord.Up | MovingFighterDriver.DashButton : Forward));
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static bool IsInvulnerable(MovingFighterDriver driver) =>
        _character.Moves[driver.Fighter.Move].IsInvulnerableOn(driver.Fighter.MoveFrame);
}
