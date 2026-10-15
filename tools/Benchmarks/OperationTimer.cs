using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Benchmarks;

/// <summary>Warms an operation up, then times it in batches and counts what it allocates.</summary>
/// <remarks>
/// One call of a toy operation takes nanoseconds, below the timer's resolution, so each sample times a batch of
/// calls sized to last about <see cref="BenchmarkOptions.TargetSampleDuration"/>. Percentiles are therefore over
/// batch averages, not single calls.
/// </remarks>
internal sealed class OperationTimer
{
    // Calls between clock reads while warming up; small enough that slow operations still stop on time.
    private const int WarmUpBatchSize = 64;

    private readonly long _warmUpTicks;
    private readonly long _targetSampleTicks;
    private readonly long[] _sampleTicks;

    public OperationTimer(BenchmarkOptions options)
    {
        _warmUpTicks = ToStopwatchTicks(options.WarmUp);
        _targetSampleTicks = ToStopwatchTicks(options.TargetSampleDuration);
        _sampleTicks = new long[options.SampleCount];
    }

    public OperationResult Measure<TOperation>(ref TOperation operation)
        where TOperation : struct, IBenchmarkOperation
    {
        int batchSize = WarmUp(ref operation);

        // Nothing between these two reads may allocate except the operation itself: the sample buffer is
        // allocated up front and the clock reads are allocation-free.
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int sample = 0; sample < _sampleTicks.Length; sample++)
        {
            long start = Stopwatch.GetTimestamp();
            RunBatch(ref operation, batchSize);
            _sampleTicks[sample] = Stopwatch.GetTimestamp() - start;
        }

        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        return OperationResult.FromSamples(operation.Name, _sampleTicks, batchSize, allocatedBytes);
    }

    // Runs long enough for the JIT to finish optimising RunBatch, then sizes the batches from the measured speed.
    private int WarmUp<TOperation>(ref TOperation operation)
        where TOperation : struct, IBenchmarkOperation
    {
        long start = Stopwatch.GetTimestamp();
        long calls = 0;
        long elapsed;
        do
        {
            RunBatch(ref operation, WarmUpBatchSize);
            calls += WarmUpBatchSize;
            elapsed = Stopwatch.GetTimestamp() - start;
        }
        while (elapsed < _warmUpTicks);

        long batchSize = _targetSampleTicks * calls / Math.Max(elapsed, 1);
        return (int)Math.Clamp(batchSize, 1, int.MaxValue);
    }

    // Not inlined, so warm-up and measurement run the same compiled loop with Run() inlined into it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunBatch<TOperation>(ref TOperation operation, int count)
        where TOperation : struct, IBenchmarkOperation
    {
        for (int call = 0; call < count; call++)
        {
            operation.Run();
        }
    }

    private static long ToStopwatchTicks(TimeSpan duration) =>
        (long)(duration.TotalSeconds * Stopwatch.Frequency);
}
