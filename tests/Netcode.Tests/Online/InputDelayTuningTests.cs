using System;
using Netcode.Online;
using Xunit;

namespace Netcode.Tests.Online;

public sealed class InputDelayTuningTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(49, 1)]
    [InlineData(50, 2)]
    [InlineData(100, 2)]
    [InlineData(int.MaxValue, 2)]
    public void OneToTwoFramesSwitchAtThePingStep(int ping, int frames)
    {
        Assert.Equal(frames, Tuning(minimumFrames: 1, maximumFrames: 2, step: 50).FramesFor(ping));
    }

    [Theory]
    [InlineData(29, 0)]
    [InlineData(95, 3)]
    [InlineData(120, 4)]
    [InlineData(500, 4)]
    public void EachFullStepAddsAFrameUpToTheMaximum(int ping, int frames)
    {
        Assert.Equal(frames, Tuning(minimumFrames: 0, maximumFrames: 4, step: 30).FramesFor(ping));
    }

    [Fact]
    public void ARangeThatRunsBackwardsIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Tuning(minimumFrames: 2, maximumFrames: 1, step: 50).Validate());
    }

    [Fact]
    public void AZeroPingStepIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Tuning(minimumFrames: 1, maximumFrames: 2, step: 0).Validate());
    }

    private static InputDelayTuning Tuning(int minimumFrames, int maximumFrames, int step) => new()
    {
        MinimumFrames = minimumFrames,
        MaximumFrames = maximumFrames,
        PingStepMilliseconds = step,
        PingSampleIntervalFrames = 60,
        PingSamples = 15,
    };
}
