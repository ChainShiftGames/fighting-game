using Sim.Characters;
using Sim.Fighters.Movement;
using Sim.Fighters.States;
using Sim.Math;
using Sim.Pipeline;
using Sim.State;
using Xunit;
using static Sim.Tests.Fighters.States.TestCharacter;

namespace Sim.Tests.Fighters.Movement;

public sealed class MovementStageTests
{
    private static readonly CharacterData _character = Create();

    [Fact]
    public void OnlyFightersThatAdvanceThisFrameMove()
    {
        MatchState state = WalkingTowardEachOther();

        new MovementStage(_character, _character).Run(ref state, new FrameActivity(1 << 1, isFrozen: true));

        Assert.Equal(-100_000, state.Fighters[0].Position.X);
        Assert.Equal(100_000 - WalkForwardSpeed, state.Fighters[1].Position.X);
    }

    [Fact]
    public void MirroredFightersMoveMirrored()
    {
        MatchState state = WalkingTowardEachOther();
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            state.Fighters[fighter].Takeoff = Takeoff.Jump;
            state.Fighters[fighter].TakeoffDirection = 1;
        }

        var stage = new MovementStage(_character, _character);
        stage.Run(ref state, FrameActivity.Everyone);
        state.Fighters[0].Takeoff = Takeoff.None;
        state.Fighters[1].Takeoff = Takeoff.None;
        stage.Run(ref state, FrameActivity.Everyone);

        Assert.Equal(state.Fighters[0].Position.MirrorX(), state.Fighters[1].Position);
        Assert.Equal(state.Fighters[0].Velocity.MirrorX(), state.Fighters[1].Velocity);
        Assert.Equal(-100_000 + (2 * JumpForwardSpeed), state.Fighters[0].Position.X);
    }

    private static MatchState WalkingTowardEachOther()
    {
        MatchState state = default;
        state.Fighters[0] = Walker(-100_000, facing: 1);
        state.Fighters[1] = Walker(100_000, facing: -1);
        return state;
    }

    private static FighterState Walker(int x, int facing)
    {
        FighterState fighter = new() { Position = new IntVector2(x, 0), Facing = facing };
        MoveEntry.EnterBasic(ref fighter, FighterAction.WalkForward, _character);
        return fighter;
    }
}
