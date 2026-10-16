using Sim.Input;
using Sim.Pipeline;
using Sim.State;
using Sim.Tests.Input;
using Xunit;

namespace Sim.Tests.Pipeline;

public sealed class ButtonSetInputStageTests
{
    [Fact]
    public void PipelineRecordsCleanedInputThroughTheButtonSet()
    {
        ButtonSet buttons = TestButtonSets.FiveButtons();
        Assert.True(buttons.TryFind("dash", out int dash));
        const ushort UndefinedBit = 1 << 9;
        TickPipeline pipeline = Pipelines.Create(new TickStages { Input = new ButtonSetInputStage(buttons) });
        MatchState state = default;

        pipeline.Tick(
            ref state,
            new InputWord((ushort)(InputWord.Left | InputWord.Right | UndefinedBit | buttons.MaskOf(dash))),
            new InputWord(InputWord.Up | InputWord.Down));

        Assert.Equal(new InputWord(buttons.MaskOf(dash)), state.Inputs[0].FramesAgo(0));
        Assert.Equal(new InputWord(InputWord.Up), state.Inputs[1].FramesAgo(0));
    }
}
