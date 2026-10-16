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

    [Theory]
    [InlineData(4, MovePhase.Startup)]
    [InlineData(5, MovePhase.Active)]
    [InlineData(7, MovePhase.Active)]
    [InlineData(8, MovePhase.Recovery)]
    public void TakesTheAnimationFrameAndPhaseFromTheMoveFrame(int moveFrame, MovePhase phase)
    {
        FighterPose pose = FighterPose.Of(_character, Fighter(FighterAction.Attack, "5a", moveFrame));

        Assert.Equal(moveFrame, pose.Frame);
        Assert.Equal(15, pose.TotalFrames);
        Assert.Equal(phase, pose.Phase);
        Assert.Equal($"5a {moveFrame}/15", pose.Label);
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
