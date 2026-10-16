using System;
using Netcode.Drivers;
using Netcode.Tests.Matches;
using Sim.Composition;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Drivers;

public sealed class ReplayMatchDriverTests
{
    private const int Seed = 8642;
    private const int Frames = 600;

    private readonly MatchSimulation _match = TestmanMirror.Create();

    [Fact]
    public void PlaysTheRecordingLikeTheMatchItRecorded()
    {
        FrameInputs[] recording = Record(Frames);
        using var driver = new ReplayMatchDriver<MatchState>(new ComposedMatchSimulation(_match), recording);
        MatchState expected = _match.StartState();

        foreach (FrameInputs inputs in recording)
        {
            Assert.True(driver.TryAdvanceFrame([]));
            _match.Tick(ref expected, inputs.Player1, inputs.Player2);
        }

        Assert.Equal(Frames, driver.CurrentFrame);
        Assert.Equal(StateChecksum.Compute(in expected), StateChecksum.Compute(in driver.State));
    }

    [Fact]
    public void StopsWhenTheRecordingRunsOut()
    {
        using var driver = new ReplayMatchDriver<MatchState>(new ComposedMatchSimulation(_match), Record(2));

        Assert.True(driver.TryAdvanceFrame([]));
        Assert.True(driver.TryAdvanceFrame([]));
        Assert.True(driver.IsFinished);
        Assert.False(driver.TryAdvanceFrame([]));
        Assert.Equal(2, driver.CurrentFrame);
    }

    [Fact]
    public void TakesNoInputFromThisPc()
    {
        using var driver = new ReplayMatchDriver<MatchState>(new ComposedMatchSimulation(_match), Record(1));

        Assert.Equal(0, driver.LocalPlayerCount);
        Assert.Throws<ArgumentException>(() => driver.TryAdvanceFrame([InputWord.Neutral]));
    }

    private static FrameInputs[] Record(int frames)
    {
        var inputs = new SeededInputs(Seed);
        var recording = new FrameInputs[frames];
        for (int frame = 0; frame < frames; frame++)
        {
            recording[frame] = new FrameInputs(inputs.Next(), inputs.Next());
        }

        return recording;
    }
}
