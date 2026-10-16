using Sim.Pipeline;
using Sim.State;
using Xunit;

namespace Sim.Tests.Pipeline;

public sealed class StageSequenceTests
{
    [Fact]
    public void StagesRunInTheGivenOrder()
    {
        MatchState state = default;

        new StageSequence(new AppendDigit(1), new AppendDigit(2)).Run(ref state, FrameActivity.Everyone);

        Assert.Equal(12, state.Frame);
    }

    // Records the run order in the frame number: each stage appends its digit.
    private sealed class AppendDigit(int digit) : ITickStage
    {
        public void Run(ref MatchState state, in FrameActivity activity) => state.Frame = (state.Frame * 10) + digit;
    }
}
