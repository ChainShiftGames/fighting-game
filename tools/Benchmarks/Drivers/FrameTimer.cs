using System;
using System.Diagnostics;

namespace Benchmarks.Drivers;

/// <summary>Warms a driver benchmark up, then times its frames one by one and counts what the process allocates.</summary>
/// <remarks>
/// A driver frame takes microseconds, well above the timer's resolution, so unlike <see cref="OperationTimer"/>
/// every sample is a single frame and p99 shows single slow frames. Allocations are counted for the whole process
/// over the sampling run, because Backdash's network jobs run on their own thread, and separately for the thread
/// that runs the frames, which is what the game's logic frame would allocate.
/// </remarks>
internal sealed class FrameTimer
{
    // Frames that must have run before timing starts, so a peer session is past its handshake and time sync.
    private const int MinWarmUpFrames = 300;

    private static readonly TimeSpan _startTimeout = TimeSpan.FromSeconds(30);

    private readonly TimeSpan _warmUp;
    private readonly long[] _sampleTicks;

    public FrameTimer(BenchmarkOptions options)
    {
        _warmUp = options.WarmUp;
        _sampleTicks = new long[options.SampleCount];
    }

    public FrameResult Measure(IDriverBenchmark benchmark)
    {
        WarmUp(benchmark);

        long waitedFrames = 0;
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        long frameThreadAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int sample = 0; sample < _sampleTicks.Length; sample++)
        {
            benchmark.BetweenFrames();
            long start = Stopwatch.GetTimestamp();
            bool advanced = benchmark.AdvanceFrame();
            _sampleTicks[sample] = Stopwatch.GetTimestamp() - start;
            waitedFrames += advanced ? 0 : 1;
        }

        long frameThreadBytes = GC.GetAllocatedBytesForCurrentThread() - frameThreadAllocatedBefore;
        long allocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        int drivers = benchmark.DriversInProcess;
        OperationResult timing = OperationResult.FromSamples(
            benchmark.Name, _sampleTicks, batchSize: 1, allocatedBytes / drivers);
        return new FrameResult(timing, frameThreadBytes / drivers, waitedFrames);
    }

    private void WarmUp(IDriverBenchmark benchmark)
    {
        var clock = Stopwatch.StartNew();
        int advancedFrames = 0;
        while (clock.Elapsed < _warmUp || advancedFrames < MinWarmUpFrames)
        {
            if (advancedFrames == 0 && clock.Elapsed > _startTimeout)
            {
                throw new TimeoutException($"{benchmark.Name} ran no frame within {_startTimeout.TotalSeconds} s.");
            }

            benchmark.BetweenFrames();
            advancedFrames += benchmark.AdvanceFrame() ? 1 : 0;
        }
    }
}
