using System;
using System.Diagnostics;

namespace Benchmarks;

/// <summary>Timing statistics (per call, in microseconds) and allocations of one measured operation.</summary>
internal sealed record OperationResult(
    string Name,
    double MeanMicroseconds,
    double MedianMicroseconds,
    double P99Microseconds,
    long Calls,
    long AllocatedBytes)
{
    private const double MicrosecondsPerSecond = 1_000_000;
    private const double P99Fraction = 0.99;

    public double BytesPerCall => (double)AllocatedBytes / Calls;

    public static OperationResult FromSamples(string name, long[] sampleTicks, int batchSize, long allocatedBytes)
    {
        double microsecondsPerTick = MicrosecondsPerSecond / Stopwatch.Frequency;
        var perCall = new double[sampleTicks.Length];
        double sum = 0;
        for (int i = 0; i < sampleTicks.Length; i++)
        {
            perCall[i] = sampleTicks[i] * microsecondsPerTick / batchSize;
            sum += perCall[i];
        }

        Array.Sort(perCall);
        int p99Index = (int)Math.Ceiling(perCall.Length * P99Fraction) - 1;
        return new OperationResult(
            name,
            sum / perCall.Length,
            perCall[perCall.Length / 2],
            perCall[p99Index],
            (long)sampleTicks.Length * batchSize,
            allocatedBytes);
    }
}
