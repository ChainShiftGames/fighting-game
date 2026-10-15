using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
