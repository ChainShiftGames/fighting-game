using System;
using Netcode.Drivers;
using Sim.Input;
using Sim.State;
using Sim.Toy;
using Xunit;

namespace Netcode.Tests.Drivers;

public sealed class LocalMatchDriverTests
{
    private const int Seed = 4321;
    private const int Frames = 600;

    private readonly ToySimulation _simulation = new(ToyRules.Default);

    [Fact]
    public void RunsEachFrameOnceWithTheGivenInputs()
    {
        using var driver = new LocalMatchDriver<ToyState>(new ToyMatchSimulation(_simulation));
        ToyState expected = _simulation.CreateInitialState();
        var inputs = new SeededInputs(Seed);

        for (int frame = 0; frame < Frames; frame++)
        {
            InputWord player1 = inputs.Next();
            InputWord player2 = inputs.Next();
            Assert.True(driver.TryAdvanceFrame([player1, player2]));
            _simulation.Tick(ref expected, player1, player2);
        }

        Assert.Equal(Frames, driver.CurrentFrame);
        Assert.Equal(StateChecksum.Compute(in expected), StateChecksum.Compute(in driver.State));
    }

    [Fact]
    public void RejectsTheWrongNumberOfInputs()
    {
        using var driver = new LocalMatchDriver<ToyState>(new ToyMatchSimulation(_simulation));

        Assert.Throws<ArgumentException>(() => driver.TryAdvanceFrame([InputWord.Neutral]));
    }
}
