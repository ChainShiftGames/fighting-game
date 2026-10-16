using System;
using FightingGame.Diagnostics;
using Xunit;

namespace FightingGame.Tests.Diagnostics;

public sealed class InputLatencyCounterTests
{
    // One timestamp tick per millisecond keeps the expected values readable.
    private const long TicksPerSecond = 1000;

    [Fact]
    public void ReadsZeroBeforeAnyFrameIsDrawn()
    {
        var counter = new InputLatencyCounter(4, TicksPerSecond);

        Assert.Equal(0, counter.AverageMilliseconds);
        Assert.Equal(0, counter.MaxMilliseconds);
    }

    [Fact]
    public void MeasuresFromInputReadToFrameDrawn()
    {
        var counter = new InputLatencyCounter(4, TicksPerSecond);

        counter.InputRead(100);
        counter.FrameDrawn(103);

        Assert.Equal(3, counter.AverageMilliseconds);
        Assert.Equal(3, counter.MaxMilliseconds);
    }

    [Fact]
    public void ConvertsTimestampTicksToMilliseconds()
    {
        var counter = new InputLatencyCounter(4, ticksPerSecond: 10_000_000);

        counter.InputRead(0);
        counter.FrameDrawn(16_000);

        Assert.Equal(1.6, counter.AverageMilliseconds, precision: 9);
    }

    [Fact]
    public void CountsTheOldestInputWhenSeveralLogicFramesRunBeforeOneDraw()
    {
        var counter = new InputLatencyCounter(4, TicksPerSecond);

        counter.InputRead(100);
        counter.InputRead(108);
        counter.FrameDrawn(110);

        Assert.Equal(10, counter.MaxMilliseconds);
    }

    [Fact]
    public void IgnoresDrawsWithoutANewInputRead()
    {
        var counter = new InputLatencyCounter(4, TicksPerSecond);

        counter.InputRead(100);
        counter.FrameDrawn(102);
        counter.FrameDrawn(150);

        Assert.Equal(2, counter.AverageMilliseconds);
        Assert.Equal(2, counter.MaxMilliseconds);
    }

    [Fact]
    public void AveragesAndTakesTheMaximumOverTheWindow()
    {
        var counter = new InputLatencyCounter(4, TicksPerSecond);

        Measure(counter, 2);
        Measure(counter, 4);
        Measure(counter, 6);

        Assert.Equal(4, counter.AverageMilliseconds);
        Assert.Equal(6, counter.MaxMilliseconds);
    }

    [Fact]
    public void DropsSamplesOlderThanTheWindow()
    {
        var counter = new InputLatencyCounter(2, TicksPerSecond);

        Measure(counter, 20);
        Measure(counter, 2);
        Measure(counter, 4);

        Assert.Equal(3, counter.AverageMilliseconds);
        Assert.Equal(4, counter.MaxMilliseconds);
    }

    [Fact]
    public void RejectsAnEmptyWindow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputLatencyCounter(0, TicksPerSecond));
    }

    private static void Measure(InputLatencyCounter counter, long milliseconds)
    {
        counter.InputRead(1000);
        counter.FrameDrawn(1000 + milliseconds);
    }
}
