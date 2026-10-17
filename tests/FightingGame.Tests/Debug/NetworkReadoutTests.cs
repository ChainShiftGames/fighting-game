using FightingGame.Debug;
using Netcode.Transport;
using Xunit;

namespace FightingGame.Tests.Debug;

public sealed class NetworkReadoutTests
{
    [Fact]
    public void ShowsNothingBeforeAnOnlineMatch()
    {
        var readout = new NetworkReadout(new LiveNetworkSimulation(NetworkSimulation.TestConditions));

        Assert.Equal(string.Empty, readout.Describe());
    }

    [Fact]
    public void FollowsTheLiveConditionsOnceWatching()
    {
        var live = new LiveNetworkSimulation();
        var readout = new NetworkReadout(live);
        readout.Watch(new DroppedPackets());

        string before = readout.Describe();
        live.Current = NetworkSimulation.TestConditions;

        Assert.Equal("network sim (F1): off | dropped on arrival: 0 malformed, 0 oversized", before);
        Assert.StartsWith("network sim (F1): 100 ms ±20 ms, 2 % loss", readout.Describe());
    }

    [Fact]
    public void CountsBothKindsOfDroppedPackets()
    {
        string text = NetworkReadout.Describe(NetworkSimulation.None with { PingMilliseconds = 60 }, malformed: 3, oversized: 1);

        Assert.Equal(
            "network sim (F1): 60 ms ±0 ms, 0 % loss on this PC's sends | dropped on arrival: 3 malformed, 1 oversized",
            text);
    }
}
