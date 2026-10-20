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

    [Fact]
    public void RecordedFramesCountAsAdvanced()
    {
        InputHistory history = Recorded(3);

        Assert.False(history.IsSkipped(0));
        Assert.False(history.IsSkipped(2));
        Assert.False(history.IsSkipped(3));
    }

    [Fact]
    public void MarkNewestSkippedMarksOnlyTheNewestFrame()
    {
        InputHistory history = Recorded(3);

        history.MarkNewestSkipped();
        history.Record(InputWord.Neutral);

        Assert.False(history.IsSkipped(0));
        Assert.True(history.IsSkipped(1));
        Assert.False(history.IsSkipped(2));
    }

    [Fact]
    public void ReusedSlotsForgetTheirSkippedMark()
    {
        InputHistory history = default;
        for (int frame = 0; frame < InputHistory.Capacity; frame++)
        {
            history.Record(InputWord.Neutral);
            history.MarkNewestSkipped();
        }

        for (int frame = 0; frame < InputHistory.Capacity; frame++)
        {
            history.Record(InputWord.Neutral);
        }

        for (int framesAgo = 0; framesAgo < InputHistory.Capacity; framesAgo++)
        {
            Assert.False(history.IsSkipped(framesAgo));
        }
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 2, 2)]
    [InlineData(3, 11, 14)]
    public void OldestFrameWithinCountsEveryFrameWithoutSkips(int newest, int ownFrames, int expected)
    {
        InputHistory history = Recorded(20);

        Assert.Equal(expected, history.OldestFrameWithin(newest, ownFrames));
    }

    [Fact]
    public void OldestFrameWithinStepsOverSkippedFrames()
    {
        // Oldest first: 2 advanced frames, 4 skipped, then the newest 2 advanced.
        InputHistory history = Recorded(2);
        for (int frame = 0; frame < 4; frame++)
        {
            history.Record(InputWord.Neutral);
            history.MarkNewestSkipped();
        }

        history.Record(InputWord.Neutral);
        history.Record(InputWord.Neutral);

        // Skipped frames right before the oldest own frame still belong to it.
        Assert.Equal(5, history.OldestFrameWithin(0, 1));
        Assert.Equal(6, history.OldestFrameWithin(0, 2));
        Assert.Equal(7, history.OldestFrameWithin(0, 3));
        Assert.Equal(5, history.OldestFrameWithin(2, 0));
    }

    [Fact]
    public void OldestFrameWithinZeroCoversEveryFrameSinceTheFighterLastAdvanced()
    {
        InputHistory history = Recorded(2);
        for (int frame = 0; frame < 3; frame++)
        {
            history.Record(InputWord.Neutral);
            history.MarkNewestSkipped();
        }

        history.Record(InputWord.Neutral);

        Assert.Equal(3, history.OldestFrameWithin(0, 0));
    }

    [Fact]
    public void OldestFrameWithinCountsUnrecordedFramesAsAdvanced()
    {
        InputHistory history = default;
        for (int frame = 0; frame < 5; frame++)
        {
            history.Record(InputWord.Neutral);
            history.MarkNewestSkipped();
        }

        // Frames 1-4 ago are skipped; 5 and 6 aren't recorded and count as the fighter's own frames.
        Assert.Equal(6, history.OldestFrameWithin(0, 2));
    }

    private static InputHistory Recorded(int frames)
    {
        InputHistory history = default;
        for (int frame = 0; frame < frames; frame++)
        {
            history.Record(InputWord.Neutral);
        }

        return history;
    }

    [Fact]
    public void NothingIsUsedAtFirst()
    {
        InputHistory history = default;
        history.Record(new InputWord(1));

        Assert.False(history.IsUsed(0));
        Assert.False(history.IsUsed(InputHistory.Capacity - 1));
    }

    [Fact]
    public void UsePressesCoversTheNewestFrameAndOlderOnly()
    {
        InputHistory history = default;
        history.Record(new InputWord(1));
        history.Record(new InputWord(2));
        history.UsePresses();
        history.Record(new InputWord(3));

        Assert.False(history.IsUsed(0));
        Assert.True(history.IsUsed(1));
        Assert.True(history.IsUsed(2));
    }

    [Fact]
    public void UsedFramesAreForgottenOnceTheyLeaveTheHistory()
    {
        InputHistory history = default;
        history.Record(new InputWord(1));
        history.UsePresses();
        for (int frame = 0; frame < InputHistory.Capacity; frame++)
        {
            history.Record(new InputWord(2));
        }

        Assert.False(history.IsUsed(InputHistory.Capacity - 1));
        Assert.False(history.IsUsed(InputHistory.Capacity));
    }
}
