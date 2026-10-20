using System;
using FightingGame.Loop;
using Xunit;

namespace FightingGame.Tests.Loop;

public sealed class FixedStepClockTests
{
    // 1000 timer ticks per 60 Hz logic frame keeps the phase readable: 1 ms is 60 ticks.
    private const long TicksPerSecond = 60_000;
    private const long Frame = 1000;
    private const int Jitter = 120;

    // Late enough to clamp the catch-up, and 0.3 frames past a boundary so a wrong restart phase would show.
    private const long Hitch = (10 * Frame) + 300;

    // An hour of 60 Hz frame callbacks.
    private const int LongRun = 216_000;

    // At 0.1 % drift, corrections are about 1000 callbacks apart; a burst of them would sit next to each other.
    private const int MinimumCorrectionSpacing = 500;

    [Fact]
    public void FirstCallOnlyStartsTheClock()
    {
        FixedStepClock clock = Clock(LoopSettings.Default);

        Assert.Equal(0, clock.Advance(12_345));
    }

    [Fact]
    public void LogicFrameBoundariesSitHalfwayBetweenFrameCallbacks()
    {
        FixedStepClock clock = Clock(WithoutTolerance());

        clock.Advance(0);

        Assert.Equal(0, clock.Advance((Frame / 2) - 1));
        Assert.Equal(1, clock.Advance(Frame / 2));
        Assert.Equal(0, clock.Advance(Frame + (Frame / 2) - 1));
        Assert.Equal(1, clock.Advance(Frame + (Frame / 2)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(250)]
    public void RunsOneFramePerFrameCallbackAt60HzWithJitter(int jitterTolerancePermille)
    {
        FixedStepClock clock = Clock(LoopSettings.Default with { JitterTolerancePermille = jitterTolerancePermille });

        AssertEveryCallbackRuns(1, clock, JitteredCallbacks(10_000, 60_000, seed: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(250)]
    public void RunsOneFramePerFrameCallbackWhenJitterAlternatesAtItsWorst(int jitterTolerancePermille)
    {
        FixedStepClock clock = Clock(LoopSettings.Default with { JitterTolerancePermille = jitterTolerancePermille });

        AssertEveryCallbackRuns(1, clock, AlternatingCallbacks(start: 0, count: 1000));
    }

    [Fact]
    public void RunsTwoFramesPerFrameCallbackAt30HzWithJitter()
    {
        FixedStepClock clock = Clock(LoopSettings.Default);

        AssertEveryCallbackRuns(2, clock, JitteredCallbacks(10_000, 30_000, seed: 2));
    }

    [Fact]
    public void ClampedCatchUpRunsTheMostFramesAllowed()
    {
        FixedStepClock clock = Clock(LoopSettings.Default);
        clock.Advance(0);

        Assert.Equal(LoopSettings.Default.MaxCatchUpFrames, clock.Advance(Hitch));
    }

    [Fact]
    public void ClampedCatchUpRestartsHalfwayBetweenFrameCallbacks()
    {
        FixedStepClock clock = Clock(WithoutTolerance());
        clock.Advance(0);
        clock.Advance(Hitch);

        Assert.Equal(0, clock.Advance(Hitch + (Frame / 2) - 1));
        Assert.Equal(1, clock.Advance(Hitch + (Frame / 2)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(250)]
    public void RunsOneFramePerFrameCallbackAfterAClampedCatchUp(int jitterTolerancePermille)
    {
        FixedStepClock clock = Clock(LoopSettings.Default with { JitterTolerancePermille = jitterTolerancePermille });
        clock.Advance(0);
        clock.Advance(Hitch);

        long[] callbacks = AlternatingCallbacks(Hitch, 1000);
        for (int callback = 1; callback < callbacks.Length; callback++)
        {
            Assert.Equal(1, clock.Advance(callbacks[callback]));
        }
    }

    [Fact]
    public void HoldsOneFramePerFrameCallbackWhileJitterStraddlesABoundary()
    {
        FixedStepClock clock = Clock(LoopSettings.Default);
        clock.Advance(0);

        // One late callback leaves a boundary 50 ticks after the 60 Hz grid, as a drifting display eventually does.
        Assert.Equal(1, clock.Advance(1450));
        long[] callbacks = AlternatingCallbacks(1450, 1000);
        for (int callback = 1; callback < callbacks.Length; callback++)
        {
            Assert.Equal(1, clock.Advance(callbacks[callback]));
        }
    }

    [Theory]
    [InlineData(59_940, 2)]
    [InlineData(60_060, 0)]
    public void DriftAgainstTheLogicRateCorrectsOneFrameAtATime(int displayMillihertz, int correctionFrames)
    {
        FixedStepClock clock = Clock(LoopSettings.Default);
        long[] callbacks = JitteredCallbacks(LongRun, displayMillihertz, seed: 3);
        clock.Advance(callbacks[0]);

        int corrections = 0;
        int? previousCorrection = null;
        for (int callback = 1; callback < callbacks.Length; callback++)
        {
            int frames = clock.Advance(callbacks[callback]);
            if (frames == 1)
            {
                continue;
            }

            Assert.Equal(correctionFrames, frames);
            Assert.True(
                previousCorrection is null || callback - previousCorrection >= MinimumCorrectionSpacing,
                $"Corrections at callbacks {previousCorrection} and {callback} are a burst.");
            previousCorrection = callback;
            corrections++;
        }

        // 0.1 % drift over an hour of callbacks.
        Assert.InRange(corrections, 200, 230);
    }

    [Theory]
    [InlineData(60_000)]
    [InlineData(59_940)]
    [InlineData(60_060)]
    [InlineData(30_000)]
    [InlineData(144_000)]
    public void TotalFramesMatchElapsedTimeOverALongRun(int displayMillihertz)
    {
        FixedStepClock clock = Clock(LoopSettings.Default);
        long[] callbacks = JitteredCallbacks(LongRun, displayMillihertz, seed: 4);

        long totalFrames = 0;
        foreach (long timestamp in callbacks)
        {
            totalFrames += clock.Advance(timestamp);
        }

        long elapsedFrames = (callbacks[^1] - callbacks[0]) / Frame;
        Assert.InRange(totalFrames, elapsedFrames - 1, elapsedFrames + 1);
    }

    [Fact]
    public void CatchesUpAfterAHitchWithoutHoldingAFrameBack()
    {
        FixedStepClock clock = Clock(LoopSettings.Default);
        clock.Advance(0);
        clock.Advance(600);

        // Two frames' worth of time lands just past the second boundary: both run now.
        Assert.Equal(2, clock.Advance(600 + (2 * Frame)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(500)]
    public void RejectsAToleranceOutsideHalfAFrame(int jitterTolerancePermille)
    {
        LoopSettings settings = LoopSettings.Default with { JitterTolerancePermille = jitterTolerancePermille };

        Assert.Throws<ArgumentOutOfRangeException>(() => Clock(settings));
    }

    private static FixedStepClock Clock(LoopSettings settings) => new(settings, TicksPerSecond);

    private static LoopSettings WithoutTolerance() => LoopSettings.Default with { JitterTolerancePermille = 0 };

    private static void AssertEveryCallbackRuns(int frames, FixedStepClock clock, long[] callbacks)
    {
        Assert.Equal(0, clock.Advance(callbacks[0]));
        for (int callback = 1; callback < callbacks.Length; callback++)
        {
            Assert.Equal(frames, clock.Advance(callbacks[callback]));
        }
    }

    // Frame callbacks of a display at the given rate, each up to ±2 ms off (seeded, so every run is the same).
    private static long[] JitteredCallbacks(int count, int displayMillihertz, int seed)
    {
        var random = new Random(seed);
        var callbacks = new long[count];
        for (int callback = 0; callback < count; callback++)
        {
            long onTime = callback * TicksPerSecond * 1000 / displayMillihertz;
            callbacks[callback] = onTime + random.Next(-Jitter, Jitter + 1);
        }

        return callbacks;
    }

    // 60 Hz callbacks from start, alternately 2 ms early and late: the jitter that flipped 0, 2, 0, 2 at the old phase.
    private static long[] AlternatingCallbacks(long start, int count)
    {
        var callbacks = new long[count];
        callbacks[0] = start;
        for (int callback = 1; callback < count; callback++)
        {
            int jitter = callback % 2 == 0 ? Jitter : -Jitter;
            callbacks[callback] = start + (callback * Frame) + jitter;
        }

        return callbacks;
    }
}
