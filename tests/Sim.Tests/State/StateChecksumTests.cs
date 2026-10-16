using Sim.Math;
using Sim.State;
using Xunit;

namespace Sim.Tests.State;

public sealed class StateChecksumTests
{
    [Fact]
    public void EqualStatesHaveEqualChecksums()
    {
        MatchState first = default;
        first.Frame = 42;
        first.Fighters[0].Health = 1000;
        MatchState copy = first;

        Assert.Equal(StateChecksum.Compute(in first), StateChecksum.Compute(in copy));
    }

    [Fact]
    public void ChangingAnyFieldChangesTheChecksum()
    {
        MatchState original = default;
        ulong baseline = StateChecksum.Compute(in original);

        MatchState frame = original;
        frame.Frame = 1;
        MatchState player2Velocity = original;
        player2Velocity.Fighters[1].Velocity = new IntVector2(0, 1);

        Assert.NotEqual(baseline, StateChecksum.Compute(in frame));
        Assert.NotEqual(baseline, StateChecksum.Compute(in player2Velocity));
    }
}
