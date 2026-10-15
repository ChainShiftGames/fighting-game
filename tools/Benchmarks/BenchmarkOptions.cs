using System;
using System.Globalization;

namespace Benchmarks;

/// <summary>Command-line options of the benchmark (developer settings; see README.md).</summary>
internal sealed record BenchmarkOptions
{
    public const string Usage =
        "Usage: Benchmarks [--max-tick-us <microseconds>] [--rollback-frames <n>] [--samples <n>] [--warmup-ms <n>] " +
        "[--backdash]";

    // ADR-0003: the default rollback window.
    private const int DefaultRollbackFrames = 8;
    private const int DefaultSampleCount = 2000;
    private const int DefaultWarmUpMilliseconds = 500;
    private const int DefaultTargetSampleMicroseconds = 100;

    // Fixed so every run, on every machine, replays the same inputs.
    private const int DefaultInputSeed = 14;

    /// <summary>Fail if the tick's p99 exceeds this. Null: report times only, gate on allocations alone.</summary>
    public double? MaxTickMicroseconds { get; init; }

    /// <summary>Also measure whole frames through Backdash's sessions. Reported only, never gated.</summary>
    public bool IncludeBackdash { get; init; }

    public int RollbackFrames { get; init; } = DefaultRollbackFrames;

    public int SampleCount { get; init; } = DefaultSampleCount;

    public TimeSpan WarmUp { get; init; } = TimeSpan.FromMilliseconds(DefaultWarmUpMilliseconds);

    public TimeSpan TargetSampleDuration { get; } = TimeSpan.FromMicroseconds(DefaultTargetSampleMicroseconds);

    public int InputSeed { get; } = DefaultInputSeed;

    public static bool TryParse(string[] args, out BenchmarkOptions options, out string error)
    {
        options = new BenchmarkOptions();
        error = string.Empty;
        for (int i = 0; i < args.Length; i++)
        {
            string name = args[i];
            if (name == "--backdash")
            {
                options = options with { IncludeBackdash = true };
                continue;
            }

            i++;
            if (i >= args.Length || !double.TryParse(args[i], CultureInfo.InvariantCulture, out double value)
                || value <= 0)
            {
                error = $"'{name}' needs a positive number.";
                return false;
            }

            if (name is "--rollback-frames" or "--samples" && value % 1 != 0)
            {
                error = $"'{name}' needs a whole number.";
                return false;
            }

            switch (name)
            {
                case "--max-tick-us":
                    options = options with { MaxTickMicroseconds = value };
                    break;
                case "--rollback-frames":
                    options = options with { RollbackFrames = (int)value };
                    break;
                case "--samples":
                    options = options with { SampleCount = (int)value };
                    break;
                case "--warmup-ms":
                    options = options with { WarmUp = TimeSpan.FromMilliseconds(value) };
                    break;
                default:
                    error = $"Unknown option '{name}'.";
                    return false;
            }
        }

        return true;
    }
}
