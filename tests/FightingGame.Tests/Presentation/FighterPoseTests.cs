using System;
using FightingGame.Presentation;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Presentation;

public sealed class FighterPoseTests
{
    private static readonly CharacterData _character = TestGameData.Testman;

    // The frames around 5a's active phase, read from its data so tuning 5a doesn't break the test (ADR-0036).
    [Theory]
    [InlineData(-1, MovePhase.Startup)]
    [InlineData(0, MovePhase.Active)]
    [InlineData(1, MovePhase.Active)]
    [InlineData(2, MovePhase.Recovery)]
    public void TakesTheAnimationFrameAndPhaseFromTheMoveFrame(int around, MovePhase phase)
    {
        MoveData move = _character.Moves[_character.MoveIds.IndexOf("5a")];
        int moveFrame = around switch
        {
            < 0 => move.FirstActiveFrame - 1,
            0 => move.FirstActiveFrame,
            1 => move.LastActiveFrame,
            _ => move.LastActiveFrame + 1,
        };

        FighterPose pose = FighterPose.Of(_character, Fighter(FighterAction.Attack, "5a", moveFrame));

        Assert.Equal(moveFrame, pose.Frame);
        Assert.Equal(move.TotalFrames, pose.TotalFrames);
        Assert.Equal(phase, pose.Phase);
        Assert.Equal(move.FirstActiveFrame, pose.FirstActiveFrame);
        Assert.Equal(move.LastActiveFrame, pose.LastActiveFrame);
        Assert.Equal($"5a {moveFrame}/{move.TotalFrames}", pose.Label);
    }

    [Fact]
    public void KnowsWhenTheFighterIsKnockedOut()
    {
        FighterState down = Fighter(FighterAction.Knockdown, "knockdown", 3) with { Health = 0 };
        FighterState hurt = down with { Health = 1 };

        Assert.True(FighterPose.Of(_character, down).IsKnockedOut);
        Assert.False(FighterPose.Of(_character, hurt).IsKnockedOut);
    }

    [Fact]
    public void CarriesTheStunAndHitstopLeft()
    {
        FighterState hit = Fighter(FighterAction.Hitstun, "hitstun", 1) with
        {
            StunFramesLeft = 13,
            Timing = new FighterTiming { HitstopFramesLeft = 8 },
        };

        FighterPose pose = FighterPose.Of(_character, hit);

        Assert.Equal(13, pose.StunFramesLeft);
        Assert.Equal(8, pose.HitstopFramesLeft);
    }

    [Fact]
    public void NamesTheActionOfABasicMove()
    {
        FighterPose pose = FighterPose.Of(_character, Fighter(FighterAction.WalkForward, "idle", 12));

        Assert.Equal(MovePhase.None, pose.Phase);
        Assert.Equal("WalkForward: idle 12/60", pose.Label);
    }

    private static FighterState Fighter(FighterAction action, string moveId, int moveFrame) => new()
    {
        Facing = 1,
        Action = action,
        Move = _character.MoveIds.IndexOf(moveId),
        MoveFrame = moveFrame,
    };
}
