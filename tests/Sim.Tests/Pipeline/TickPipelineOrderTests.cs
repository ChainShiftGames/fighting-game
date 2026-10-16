using System.Collections.Generic;
using Sim.Input;
using Sim.Modules;
using Sim.Pipeline;
using Sim.State;
using Xunit;

namespace Sim.Tests.Pipeline;

public sealed class TickPipelineOrderTests
{
    [Fact]
    public void StagesAndHooksRunInTheFixedTickOrder()
    {
        var log = new List<string>();
        var stages = new TickStages
        {
            Input = new LoggingInputStage(log),
            StateMachines = new LoggingStage("state machines", log),
            Movement = new LoggingStage("movement", log),
            Collision = new LoggingStage("collision", log),
            HitDetection = new LoggingStage("hit detection", log),
            HitResolution = new LoggingStage("hit resolution", log),
            RoundTimer = new LoggingStage("round timer", log),
        };
        var module = new ScriptedModule(1)
        {
            OnAfterInput = _ => log.Add("after input hook"),
            OnAfterStateMachines = _ => log.Add("after state machines hook"),
            OnEndOfFrame = _ => log.Add("end of frame hook"),
        };
        TickPipeline pipeline = Pipelines.Create(stages, module);
        MatchState state = default;

        pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);

        Assert.Equal(
            [
                "input", "after input hook", "state machines", "after state machines hook", "movement", "collision",
                "hit detection", "hit resolution", "end of frame hook", "round timer",
            ],
            log);
    }

    [Fact]
    public void EachTickAdvancesTheFrameNumber()
    {
        TickPipeline pipeline = Pipelines.Create();
        MatchState state = default;

        pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);

        Assert.Equal(2, state.Frame);
    }

    [Fact]
    public void TheStandInInputStageAppliesSocdAndRecordsBothPlayers()
    {
        TickPipeline pipeline = Pipelines.Create();
        MatchState state = default;

        pipeline.Tick(
            ref state,
            new InputWord(InputWord.Left | InputWord.Right),
            new InputWord(InputWord.Up | InputWord.Down));

        Assert.Equal(InputWord.Neutral, state.Inputs[0].FramesAgo(0));
        Assert.Equal(new InputWord(InputWord.Up), state.Inputs[1].FramesAgo(0));
    }

    [Fact]
    public void InputIsRecordedDuringAFreeze()
    {
        var freezer = new ScriptedModule(1)
        {
            OnAfterInput = context =>
            {
                if (context.State.Frame == 0)
                {
                    context.RequestFreeze(new FreezeRequest { Frames = 5 });
                }
            },
        };
        TickPipeline pipeline = Pipelines.Create(freezer);
        MatchState state = default;

        for (int frame = 0; frame < 3; frame++)
        {
            pipeline.Tick(ref state, new InputWord(InputWord.Down), InputWord.Neutral);
        }

        Assert.True(state.Freeze.FramesLeft > 0);
        Assert.Equal(3, state.Inputs[0].Count);
    }

    private sealed class LoggingStage(string name, List<string> log) : ITickStage
    {
        public void Run(ref MatchState state, in FrameActivity activity) => log.Add(name);
    }

    private sealed class LoggingInputStage(List<string> log) : IInputStage
    {
        public void Run(ref MatchState state, InputWord player1, InputWord player2) => log.Add("input");
    }
}
