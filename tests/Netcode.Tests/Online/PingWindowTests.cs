using System;
using Netcode.Online;
using Xunit;

namespace Netcode.Tests.Online;

public sealed class PingWindowTests
{
    [Fact]
    public void AnEmptyWindowHasNoMedian()
    {
        Assert.False(new PingWindow(5).TryGetMedian(out _));
    }

    [Fact]
    public void OneSpikeDoesNotMoveTheMedian()
    {
        PingWindow window = Filled(5, 40, 42, 900, 41, 40);

        Assert.True(window.TryGetMedian(out int ping));
        Assert.Equal(41, ping);
    }

    [Fact]
    public void OneTooLowReadingDoesNotMoveTheMedian()
    {
        // ADR-0014 problem 4: Backdash's first readings can be 4–5 ms on a ~40 ms connection.
        PingWindow window = Filled(5, 4, 40, 42, 41, 40);

        Assert.True(window.TryGetMedian(out int ping));
        Assert.Equal(40, ping);
    }

    [Fact]
    public void AnEvenCountTakesTheLowerMiddleReading()
    {
        PingWindow window = Filled(4, 10, 40, 30, 20);

        Assert.True(window.TryGetMedian(out int ping));
        Assert.Equal(20, ping);
    }

    [Fact]
    public void OnlyTheNewestReadingsCount()
    {
        PingWindow window = Filled(3, 100, 100, 100, 20, 20);

        Assert.True(window.TryGetMedian(out int ping));
        Assert.Equal(20, ping);
        Assert.Equal(3, window.Count);
    }

    [Fact]
    public void AWindowNeedsRoomForOneReading()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PingWindow(0));
    }

    private static PingWindow Filled(int size, params int[] readings)
    {
        var window = new PingWindow(size);
        foreach (int reading in readings)
        {
            window.Add(reading);
        }

        return window;
    }
}
