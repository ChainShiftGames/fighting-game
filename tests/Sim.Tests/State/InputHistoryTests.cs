using Sim.Input;
using Sim.State;
using Xunit;

namespace Sim.Tests.State;

public sealed class InputHistoryTests
{
    [Fact]
    public void FramesAgoCountsBackFromTheNewest()
    {
        InputHistory history = default;
        history.Record(new InputWord(1));
        history.Record(new InputWord(2));
        history.Record(new InputWord(3));

        Assert.Equal(3, history.Count);
        Assert.Equal(new InputWord(3), history.FramesAgo(0));
        Assert.Equal(new InputWord(2), history.FramesAgo(1));
        Assert.Equal(new InputWord(1), history.FramesAgo(2));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(InputHistory.Capacity)]
    public void UnrecordedFramesAreNeutral(int framesAgo)
    {
        InputHistory history = default;
        history.Record(new InputWord(InputWord.Up));

        Assert.Equal(InputWord.Neutral, history.FramesAgo(framesAgo));
    }

    [Fact]
    public void OldestFramesAreOverwrittenWhenFull()
    {
        InputHistory history = default;
        int recorded = InputHistory.Capacity + 5;
        for (int frame = 1; frame <= recorded; frame++)
        {
            history.Record(new InputWord((ushort)frame));
        }

        Assert.Equal(InputHistory.Capacity, history.Count);
        Assert.Equal(new InputWord((ushort)recorded), history.FramesAgo(0));
        Assert.Equal(new InputWord(6), history.FramesAgo(InputHistory.Capacity - 1));
        Assert.Equal(InputWord.Neutral, history.FramesAgo(InputHistory.Capacity));
    }
}
