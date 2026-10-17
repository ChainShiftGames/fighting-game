using Sim.Characters;
using Sim.Diagnostics;
using Sim.Fighters.States;
using Sim.State;
using Sim.Tests.Fighters.States;
using Xunit;

namespace Sim.Tests.Diagnostics;

public sealed class FrameStatesTests
{
    private static readonly CharacterData _character = TestCharacter.Create();

    [Theory]
    [InlineData(1, FrameKind.Startup)]
    [InlineData(4, FrameKind.Startup)]
    [InlineData(5, FrameKind.Active)]
    [InlineData(7, FrameKind.Active)]
    [InlineData(8, FrameKind.Recovery)]
    [InlineData(15, FrameKind.Recovery)]
    public void An5AGoesThroughStartupActiveAndRecovery(int moveFrame, FrameKind expected)
    {
        // TestCharacter's 5a: startup 5, active 3, recovery 8.
        FighterState fighter = In(FighterAction.Attack, TestCharacter.Normal5A, moveFrame);

        Assert.Equal(new FighterFrameState(expected, false), FrameStates.Of(in fighter, _character));
    }

    [Theory]
    [InlineData(FighterAction.Idle, FrameKind.Free)]
    [InlineData(FighterAction.Crouch, FrameKind.Free)]
    [InlineData(FighterAction.WalkBack, FrameKind.Free)]
    [InlineData(FighterAction.Jump, FrameKind.Free)]
    [InlineData(FighterAction.Prejump, FrameKind.Movement)]
    [InlineData(FighterAction.Dash, FrameKind.Movement)]
    [InlineData(FighterAction.Landing, FrameKind.Recovery)]
    [InlineData(FighterAction.Hitstun, FrameKind.Hitstun)]
    [InlineData(FighterAction.AirHitstun, FrameKind.Hitstun)]
    [InlineData(FighterAction.Thrown, FrameKind.Hitstun)]
    [InlineData(FighterAction.CrouchBlockstun, FrameKind.Blockstun)]
    [InlineData(FighterAction.AirBlockstun, FrameKind.Blockstun)]
    [InlineData(FighterAction.Knockdown, FrameKind.Knockdown)]
    [InlineData(FighterAction.WakeUp, FrameKind.Knockdown)]
    public void BasicActionsHaveTheirKind(FighterAction action, FrameKind expected)
    {
        FighterState fighter = In(action, _character.BasicStateMove(BasicState.Idle), moveFrame: 1);

        Assert.Equal(expected, FrameStates.Of(in fighter, _character).Kind);
    }

    [Fact]
    public void ABackdashIsInvincibleForItsInvulnerableFramesOnly()
    {
        int backdash = _character.BasicStateMove(BasicState.Backdash);

        Assert.True(Of(FighterAction.Backdash, backdash, TestCharacter.BackdashInvulnerableFrames).IsInvincible);
        Assert.False(Of(FighterAction.Backdash, backdash, TestCharacter.BackdashInvulnerableFrames + 1).IsInvincible);
    }

    private static FighterFrameState Of(FighterAction action, int move, int moveFrame)
    {
        FighterState fighter = In(action, move, moveFrame);
        return FrameStates.Of(in fighter, _character);
    }

    private static FighterState In(FighterAction action, int move, int moveFrame) =>
        new() { Action = action, Move = move, MoveFrame = moveFrame };
}
