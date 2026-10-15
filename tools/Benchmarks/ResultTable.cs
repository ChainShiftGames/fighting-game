using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Benchmarks.Drivers;
using Sim.Toy;

namespace Benchmarks;

/// <summary>Writes the machine description and results as a Markdown table, ready to paste into README.md.</summary>
internal static class ResultTable
{
    public static void Write(TextWriter output, IReadOnlyList<OperationResult> results)
    {
        output.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription}, {RuntimeInformation.OSDescription}, " +
            $"{RuntimeInformation.ProcessArchitecture}, {Environment.ProcessorCount} logical cores");
        if (IsOptimizerDisabled(typeof(ToySimulation).Assembly))
        {
            output.WriteLine("WARNING: Sim was built without optimisations; run with -c Release for real numbers.");
        }

        output.WriteLine();
        WriteRows(output, results);
    }

    /// <summary>The Backdash frames, per match driver: reported only, never gated (see README.md).</summary>
    public static void WriteBackdash(TextWriter output, IReadOnlyList<FrameResult> results)
    {
        output.WriteLine();
        output.WriteLine("Whole frames through RollbackMatchDriver and Backdash (report only, not gated):");
        output.WriteLine();
        var timings = new List<OperationResult>(results.Count);
        foreach (FrameResult result in results)
        {
            timings.Add(result.Timing);
        }

        WriteRows(output, timings);
        foreach (FrameResult result in results)
        {
            if (result.WaitedFrames > 0)
            {
                output.WriteLine($"Note: {result.WaitedFrames} timed calls of {result.Timing.Name} only waited.");
            }

            if (result.Timing.AllocatedBytes > 0)
            {
                output.WriteLine($"Note: {result.Timing.Name} allocated {result.Timing.AllocatedBytes} bytes per " +
                    $"driver, {result.FrameThreadBytes} of them on the frame thread, the rest in Backdash's " +
                    "background threads.");
            }
        }
    }

    private static void WriteRows(TextWriter output, IReadOnlyList<OperationResult> results)
    {
        output.WriteLine("| Operation | Mean µs | Median µs | p99 µs | Bytes/call | Calls |");
        output.WriteLine("|---|---:|---:|---:|---:|---:|");
        foreach (OperationResult result in results)
        {
            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| {result.Name} | {result.MeanMicroseconds:F4} | {result.MedianMicroseconds:F4} | " +
                $"{result.P99Microseconds:F4} | {result.BytesPerCall:0.###} | {result.Calls} |"));
        }
    }

    private static bool IsOptimizerDisabled(Assembly assembly) =>
        assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled == true;
}
