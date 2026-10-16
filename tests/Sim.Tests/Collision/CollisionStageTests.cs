using Sim.Characters;
using Sim.Collision;
using Sim.Fighters.States;
using Sim.Pipeline;
using Sim.State;
using Xunit;
using static Sim.Tests.Collision.BoxedCharacter;

namespace Sim.Tests.Collision;

public sealed class CollisionStageTests
{
    private const int Wall = 300_000;

    private static readonly CharacterData _character = Create();

    [Fact]
    public void AGroundedFreeFighterTurnsOnceTheOpponentIsOnItsOtherSide()
    {
        MatchState state = Crossed(FighterAction.Idle);

        Stage().Run(ref state, FrameActivity.Everyone);

        Assert.Equal(-1, state.Fighters[0].Facing);
    }

    [Theory]
    [InlineData(FighterAction.Jump)]
    [InlineData(FighterAction.Attack)]
    [InlineData(FighterAction.Dash)]
    [InlineData(FighterAction.Hitstun)]
    public void FightersThatAreBusyKeepTheirFacing(FighterAction action)
    {
        MatchState state = Crossed(action);

        Stage().Run(ref state, FrameActivity.Everyone);

        Assert.Equal(1, state.Fighters[0].Facing);
    }

    [Fact]
    public void AnAirborneFighterKeepsItsFacing()
    {
        MatchState state = Crossed(FighterAction.Idle);
        state.Fighters[0].IsAirborne = 1;

        Stage().Run(ref state, FrameActivity.Everyone);

        Assert.Equal(1, state.Fighters[0].Facing);
    }

    [Fact]
    public void FightersThatDontAdvanceArePushedButDontTurn()
    {
        MatchState state = default;
        state.Fighters[0] = At(10_000, facing: 1);
        state.Fighters[1] = At(0, facing: 1);

        Stage().Run(ref state, new FrameActivity(1 << 1, isFrozen: false));

        Assert.Equal(2 * PushHalfWidth, state.Fighters[0].Position.X - state.Fighters[1].Position.X);
        Assert.Equal(1, state.Fighters[0].Facing);
        Assert.Equal(1, state.Fighters[1].Facing);
    }

    [Fact]
    public void BothFightersTurnFromTheSamePositions()
    {
        MatchState state = default;
        state.Fighters[0] = At(100_000, facing: 1);
        state.Fighters[1] = At(-100_000, facing: -1);

        Stage().Run(ref state, FrameActivity.Everyone);

        Assert.Equal(-1, state.Fighters[0].Facing);
        Assert.Equal(1, state.Fighters[1].Facing);
    }

    private static CollisionStage Stage() => new(_character, _character, new StageWalls(-Wall, Wall));

    // P1 faces right with P2 already on its left, far enough apart not to push.
    private static MatchState Crossed(FighterAction player1Action)
    {
        MatchState state = default;
        state.Fighters[0] = At(100_000, facing: 1);
        state.Fighters[0].Action = player1Action;
        state.Fighters[1] = At(-100_000, facing: 1);
        return state;
    }
}
