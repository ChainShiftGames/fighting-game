using System;
using Netcode.Drivers;
using Sim.Input;
using Sim.State;
using Sim.Toy;
using Xunit;

namespace Netcode.Tests.Drivers;

public sealed class ReplayMatchDriverTests
{
    private const int Seed = 8642;
    private const int Frames = 600;

    private readonly ToySimulation _simulation = new(ToyRules.Default);

    [Fact]
    public void PlaysTheRecordingLikeTheMatchItRecorded()
    {
        FrameInputs[] recording = Record(Frames);
        using var driver = new ReplayMatchDriver<ToyState>(new ToyMatchSimulation(_simulation), recording);
        ToyState expected = _simulation.CreateInitialState();

        foreach (FrameInputs inputs in recording)
        {
            Assert.True(driver.TryAdvanceFrame([]));
            _simulation.Tick(ref expected, inputs.Player1, inputs.Player2);
        }

        Assert.Equal(Frames, driver.CurrentFrame);
        Assert.Equal(StateChecksum.Compute(in expected), StateChecksum.Compute(in driver.State));
    }

    [Fact]
    public void StopsWhenTheRecordingRunsOut()
    {
        using var driver = new ReplayMatchDriver<ToyState>(new ToyMatchSimulation(_simulation), Record(2));

        Assert.True(driver.TryAdvanceFrame([]));
        Assert.True(driver.TryAdvanceFrame([]));
        Assert.True(driver.IsFinished);
        Assert.False(driver.TryAdvanceFrame([]));
        Assert.Equal(2, driver.CurrentFrame);
    }

    [Fact]
    public void TakesNoInputFromThisPc()
    {
        using var driver = new ReplayMatchDriver<ToyState>(new ToyMatchSimulation(_simulation), Record(1));

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
