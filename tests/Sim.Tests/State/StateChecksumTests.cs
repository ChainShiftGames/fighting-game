using Sim.State;
using Sim.Toy;
using Xunit;

namespace Sim.Tests.State;

public sealed class StateChecksumTests
{
    [Fact]
    public void EqualStatesHaveEqualChecksums()
    {
        ToyState first = new ToySimulation(ToyRules.Default).CreateInitialState();
        ToyState copy = first;

        Assert.Equal(StateChecksum.Compute(in first), StateChecksum.Compute(in copy));
    }

    [Fact]
    public void ChangingAnyFieldChangesTheChecksum()
    {
        ToyState original = default;
        ulong baseline = StateChecksum.Compute(in original);

        ToyState frame = original;
        frame.Frame = 1;
        ToyState player2Velocity = original;
        player2Velocity.Fighters[1].VelocityY = 1;

        Assert.NotEqual(baseline, StateChecksum.Compute(in frame));
        Assert.NotEqual(baseline, StateChecksum.Compute(in player2Velocity));
    }
}
