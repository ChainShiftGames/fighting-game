using Sim.Characters;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Modules;
using Sim.Pipeline;
using Sim.State;
using Sim.Tests.Pipeline;
using Xunit;
using static Sim.Tests.Fighters.States.TestCharacter;

namespace Sim.Tests.Fighters.States;

public sealed class StateMachineStageTests
{
    private static readonly CharacterData _character = Create();

    [Fact]
    public void OnlyFightersThatAdvanceThisFrameMoveOn()
    {
        var stage = new StateMachineStage(new ButtonPressMoveInput(), _character, _character);
        MatchState state = CreateIdleState(stage);
        state.Inputs[0].Record(new InputWord(ButtonA));
        state.Inputs[1].Record(new InputWord(ButtonA));

        stage.Run(ref state, new FrameActivity(advancingFighters: 1 << 1, isFrozen: true));

        Assert.Equal(FighterAction.Idle, state.Fighters[0].Action);
        Assert.Equal(FighterAction.Attack, state.Fighters[1].Action);
    }

    [Fact]
    public void StartingAMoveUsesUpTheFightersPressesOnly()
    {
        var stage = new StateMachineStage(new ButtonPressMoveInput(), _character, _character);
        MatchState state = CreateIdleState(stage);
        state.Inputs[0].Record(new InputWord(ButtonA));
        state.Inputs[1].Record(InputWord.Neutral);

        stage.Run(ref state, FrameActivity.Everyone);

        Assert.Equal(FighterAction.Attack, state.Fighters[0].Action);
        Assert.True(state.Inputs[0].IsUsed(0));
        Assert.False(state.Inputs[1].IsUsed(0));
    }

    [Fact]
    public void FollowingTheHeldDirectionUsesNoPress()
    {
        var stage = new StateMachineStage(new ButtonPressMoveInput(), _character, _character);
        MatchState state = CreateIdleState(stage);
        state.Inputs[0].Record(new InputWord(InputWord.Up));
        state.Inputs[1].Record(new InputWord(InputWord.Right));

        stage.Run(ref state, FrameActivity.Everyone);

        Assert.Equal(FighterAction.Prejump, state.Fighters[0].Action);
        Assert.False(state.Inputs[0].IsUsed(0));
        Assert.False(state.Inputs[1].IsUsed(0));
    }

    [Fact]
    public void BothFightersGetTheSameResultForTheSameInput()
    {
        var stage = new StateMachineStage(new ButtonPressMoveInput(), _character, _character);
        MatchState state = CreateIdleState(stage);
        state.Fighters[1].Facing = -1;
        ushort[] player1Inputs = [InputWord.Right, InputWord.Down | ButtonB, 0, ButtonA, InputWord.Up];
        for (int frame = 0; frame < 40; frame++)
        {
            ushort player1 = player1Inputs[frame % player1Inputs.Length];
            state.Inputs[0].Record(new InputWord(player1));
            state.Inputs[1].Record(new InputWord(MirrorHorizontal(player1)));
            stage.Run(ref state, FrameActivity.Everyone);

            FighterState mirrored = state.Fighters[1];
            mirrored.Facing = -mirrored.Facing;
            Assert.Equal(state.Fighters[0], mirrored);
        }
    }

    [Fact]
    public void HitstopFromTheCoreFreezesTheFighterInItsMove()
    {
        var stage = new StateMachineStage(new ButtonPressMoveInput(), _character, _character);
        var hitstop = new ScriptedModule(1)
        {
            OnEndOfFrame = context =>
            {
                if (context.State.Frame == 2)
                {
                    context.RequestHitstopOverride(0, new HitstopOverrideRequest { Frames = 5 });
                }
            },
        };
        TickPipeline pipeline = Pipelines.Create(new TickStages { StateMachines = stage }, hitstop);
        MatchState state = CreateIdleState(stage);

        pipeline.Tick(ref state, new InputWord(ButtonA), InputWord.Neutral);
        pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        Assert.Equal(3, state.Fighters[0].MoveFrame);
        for (int frame = 0; frame < 5; frame++)
        {
            pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        }

        Assert.Equal(3, state.Fighters[0].MoveFrame);
        pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        Assert.Equal(4, state.Fighters[0].MoveFrame);
    }

    private static MatchState CreateIdleState(StateMachineStage stage)
    {
        MatchState state = default;
        state.Fighters[0].Facing = 1;
        state.Fighters[1].Facing = 1;
        stage.EnterIdle(ref state);
        return state;
    }

    private static ushort MirrorHorizontal(ushort input)
    {
        int bits = input & ~(InputWord.Left | InputWord.Right);
        bits |= (input & InputWord.Left) != 0 ? InputWord.Right : 0;
        bits |= (input & InputWord.Right) != 0 ? InputWord.Left : 0;
        return (ushort)bits;
    }
}
