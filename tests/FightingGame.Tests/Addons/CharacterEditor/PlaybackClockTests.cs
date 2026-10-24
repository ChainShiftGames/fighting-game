using FightingGame.Addons.CharacterEditor.Documents;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

public sealed class PlaybackClockTests
{
    private const double OneFrame = 1d / PlaybackClock.FramesPerSecond;

    [Fact]
    public void ShowsOneFramePerLogicFrame()
    {
        var clock = new PlaybackClock(endHoldFrames: 0) { TotalFrames = 15 };

        Assert.Equal(1, clock.Frame);
        Assert.Equal(2, clock.Advance(OneFrame));
        Assert.Equal(12, clock.Advance(10 * OneFrame));
    }

    [Fact]
    public void HoldsTheLastFrameThenStartsOver()
    {
        var clock = new PlaybackClock(endHoldFrames: 5) { TotalFrames = 15 };

        Assert.Equal(15, clock.Advance(14 * OneFrame));
        Assert.Equal(15, clock.Advance(4 * OneFrame));
        Assert.Equal(1, clock.Advance(2 * OneFrame));
    }

    [Fact]
    public void PlaysSlowerAtALowerSpeed()
    {
        var clock = new PlaybackClock(endHoldFrames: 0) { TotalFrames = 15, Speed = 0.25d };

        Assert.Equal(1, clock.Advance(3 * OneFrame));
        Assert.Equal(2, clock.Advance(OneFrame));
    }

    [Fact]
    public void StartsFromThePickedFrameOrFromTheTopAtTheEnd()
    {
        var clock = new PlaybackClock(endHoldFrames: 0) { TotalFrames = 15 };

        clock.StartAt(8);
        Assert.Equal(8, clock.Frame);
        clock.StartAt(15);
        Assert.Equal(1, clock.Frame);
    }
}
