using System;
using Netcode.Drivers;
using Netcode.Tests.Matches;
using Sim.Composition;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Drivers;

public sealed class LocalMatchDriverTests
{
    private const int Seed = 4321;
    private const int Frames = 600;

    private readonly MatchSimulation _match = TestmanMirror.Create();

    [Fact]
    public void RunsEachFrameOnceWithTheGivenInputs()
    {
        using var driver = new LocalMatchDriver<MatchState>(new ComposedMatchSimulation(_match));
        MatchState expected = _match.StartState();
        var inputs = new SeededInputs(Seed);

        for (int frame = 0; frame < Frames; frame++)
        {
            InputWord player1 = inputs.Next();
            InputWord player2 = inputs.Next();
            Assert.True(driver.TryAdvanceFrame([player1, player2]));
            _match.Tick(ref expected, player1, player2);
        }

        Assert.Equal(Frames, driver.CurrentFrame);
        Assert.Equal(StateChecksum.Compute(in expected), StateChecksum.Compute(in driver.State));
    }

    [Fact]
    public void RejectsTheWrongNumberOfInputs()
    {
        using var driver = new LocalMatchDriver<MatchState>(new ComposedMatchSimulation(_match));

        Assert.Throws<ArgumentException>(() => driver.TryAdvanceFrame([InputWord.Neutral]));
    }
}
