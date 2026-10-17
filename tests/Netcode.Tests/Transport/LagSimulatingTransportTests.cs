using System;
using System.Diagnostics;
using System.Threading;
using Netcode.Transport;
using Xunit;

namespace Netcode.Tests.Transport;

/// <summary>P2-11: the simulated conditions follow the debug panel from the next packet on.</summary>
public sealed class LagSimulatingTransportTests
{
    // Long enough that a delayed packet can't have left before the test looks.
    private const int SlowPingMilliseconds = 4000;

    private static readonly TimeSpan _arrivalTimeout = TimeSpan.FromSeconds(5);
    private static readonly byte[] _packet = [1, 2, 3];

    [Fact]
    public void FollowingZeroConditionsSendsStraightThrough()
    {
        using var inner = new InMemoryTransport();
        using var transport = new LagSimulatingTransport(inner, NetworkSimulation.Following(new LiveNetworkSimulation()));

        transport.Send(_packet);

        Assert.Single(inner.Sent);
    }

    [Fact]
    public void ALossChangeAppliesFromTheNextPacket()
    {
        using var inner = new InMemoryTransport();
        var live = new LiveNetworkSimulation();
        using var transport = new LagSimulatingTransport(inner, NetworkSimulation.Following(live));

        transport.Send(_packet);
        live.Current = NetworkSimulation.None with { PacketLossPercent = 100 };
        for (int i = 0; i < 10; i++)
        {
            transport.Send(_packet);
        }

        live.Current = NetworkSimulation.None;
        transport.Send(_packet);

        Assert.Equal(2, inner.Sent.Count);
    }

    [Fact]
    public void APingChangeAppliesFromTheNextPacket()
    {
        using var inner = new InMemoryTransport();
        var live = new LiveNetworkSimulation(NetworkSimulation.None with { PingMilliseconds = SlowPingMilliseconds });
        using var transport = new LagSimulatingTransport(inner, NetworkSimulation.Following(live));

        transport.Send(_packet);
        Assert.Empty(inner.Sent);

        live.Current = NetworkSimulation.None;
        transport.Send(_packet);
        Assert.Single(inner.Sent);
    }

    [Fact]
    public void DelayedPacketsArrive()
    {
        using var inner = new InMemoryTransport();
        using var transport = new LagSimulatingTransport(inner, NetworkSimulation.TestConditions with { PacketLossPercent = 0 });

        transport.Send(_packet);

        var clock = Stopwatch.StartNew();
        while (inner.Sent.IsEmpty && clock.Elapsed < _arrivalTimeout)
        {
            Thread.Sleep(1);
        }

        Assert.Single(inner.Sent);
    }

    [Fact]
    public void FixedConditionsKeepDroppingAsBefore()
    {
        using var inner = new InMemoryTransport();
        using var transport = new LagSimulatingTransport(inner, NetworkSimulation.None with { PacketLossPercent = 100 });

        for (int i = 0; i < 10; i++)
        {
            transport.Send(_packet);
        }

        Assert.Empty(inner.Sent);
    }
}
