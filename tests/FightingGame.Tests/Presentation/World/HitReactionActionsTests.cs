using FightingGame.Presentation.World;
using Sim.Fighters.States;
using Xunit;

namespace FightingGame.Tests.Presentation.World;

public sealed class HitReactionActionsTests
{
    [Theory]
    [InlineData(FighterAction.Hitstun)]
    [InlineData(FighterAction.CrouchHitstun)]
    [InlineData(FighterAction.AirHitstun)]
    [InlineData(FighterAction.Blockstun)]
    [InlineData(FighterAction.CrouchBlockstun)]
    [InlineData(FighterAction.AirBlockstun)]
    public void IncludesEveryHitstunAndBlockstun(FighterAction action)
    {
        Assert.True(HitReactionActions.Includes(action));
    }

    [Theory]
    [InlineData(FighterAction.Idle)]
    [InlineData(FighterAction.Crouch)]
    [InlineData(FighterAction.WalkForward)]
    [InlineData(FighterAction.WalkBack)]
    [InlineData(FighterAction.Jump)]
    [InlineData(FighterAction.Dash)]
    [InlineData(FighterAction.Knockdown)]
    [InlineData(FighterAction.Thrown)]
    [InlineData(FighterAction.Attack)]
    public void LeavesOtherActionsOnTheirOwnClock(FighterAction action)
    {
        // Idle, walk and the other loops keep counting by the match clock and fading in.
        Assert.False(HitReactionActions.Includes(action));
    }
}
