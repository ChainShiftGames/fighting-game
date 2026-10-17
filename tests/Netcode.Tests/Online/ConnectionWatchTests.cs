using System;
using Netcode.Online;
using Xunit;

namespace Netcode.Tests.Online;

public sealed class ConnectionWatchTests
{
    private readonly ManualClock _clock = new();
    private readonly ConnectionWatch _watch;

    public ConnectionWatchTests()
    {
        _watch = new ConnectionWatch(_clock);
    }

    [Fact]
    public void StartsConnected() => Assert.Equal(ConnectionStatus.Connected, _watch.ReadConnectionStatus());

    [Fact]
    public void AnInterruptionCountsDownTheWholeSecondsLeft()
    {
        _watch.Interrupted(TimeSpan.FromSeconds(7));
        Assert.Equal(new ConnectionStatus(ConnectionState.Interrupted, 7), _watch.ReadConnectionStatus());

        _clock.Advance(TimeSpan.FromMilliseconds(500));
        Assert.Equal(7, _watch.ReadConnectionStatus().SecondsUntilDisconnect);

        _clock.Advance(TimeSpan.FromMilliseconds(6400));
        Assert.Equal(1, _watch.ReadConnectionStatus().SecondsUntilDisconnect);

        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(0, _watch.ReadConnectionStatus().SecondsUntilDisconnect);
    }

    [Fact]
    public void AResumedConnectionIsConnectedAgain()
    {
        _watch.Interrupted(TimeSpan.FromSeconds(7));
        _watch.Resumed();

        Assert.Equal(ConnectionStatus.Connected, _watch.ReadConnectionStatus());
    }

    [Fact]
    public void ADisconnectIsFinal()
    {
        _watch.Disconnected();
        _watch.Interrupted(TimeSpan.FromSeconds(7));
        _watch.Resumed();

        Assert.Equal(ConnectionStatus.Disconnected, _watch.ReadConnectionStatus());
        Assert.True(_watch.IsDisconnected);
    }

    private sealed class ManualClock : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan time) => _timestamp += time.Ticks;
    }
}
