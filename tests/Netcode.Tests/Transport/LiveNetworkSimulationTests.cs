using System;
using Netcode.Transport;
using Xunit;

namespace Netcode.Tests.Transport;

public sealed class LiveNetworkSimulationTests
{
    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    [InlineData(0, 0, 101)]
    public void RejectsNegativeTimesAndLossOutsideAPercentage(int ping, int jitter, int loss)
    {
        var live = new LiveNetworkSimulation();
        var conditions = new NetworkSimulation { PingMilliseconds = ping, JitterMilliseconds = jitter, PacketLossPercent = loss };

        Assert.Throws<ArgumentOutOfRangeException>(() => live.Current = conditions);
        Assert.Equal(NetworkSimulation.None, live.Current);
    }

    [Fact]
    public void FollowingCarriesTheCurrentValuesAndIsNeverNone()
    {
        var live = new LiveNetworkSimulation();

        NetworkSimulation following = NetworkSimulation.Following(live);

        Assert.False(following.IsNone);
        Assert.Same(live, following.Live);
        Assert.Equal(0, following.PingMilliseconds);
    }

    [Fact]
    public void KeepsOnlyTheValuesOfASetThatFollowsAnother()
    {
        var other = new LiveNetworkSimulation(NetworkSimulation.TestConditions);

        var live = new LiveNetworkSimulation(NetworkSimulation.Following(other));

        Assert.Equal(NetworkSimulation.TestConditions, live.Current);
        Assert.Null(live.Current.Live);
    }
}
