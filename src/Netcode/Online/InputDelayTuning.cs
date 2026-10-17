using System;

namespace Netcode.Online;

/// <summary>
/// How the automatic input delay follows the ping (ADR-0005 §4). Developer settings, from
/// <c>data/netcode/online.json</c>.
/// </summary>
/// <remarks>
/// The ping is Backdash's, which reads ~20 ms above the transport's own (ADR-0014 problem 4), so the step is tuned
/// against Backdash's numbers.
/// </remarks>
public sealed record InputDelayTuning
{
    /// <summary>Delay at the lowest ping.</summary>
    public required int MinimumFrames { get; init; }

    /// <summary>Delay the ping can raise it to at most; also the first round's delay, before any ping is known.</summary>
    public required int MaximumFrames { get; init; }

    /// <summary>Each full step of this much averaged ping adds one frame above <see cref="MinimumFrames"/>.</summary>
    public required int PingStepMilliseconds { get; init; }

    /// <summary>Logic frames between two ping readings.</summary>
    public required int PingSampleIntervalFrames { get; init; }

    /// <summary>Readings the average covers: the newest this many.</summary>
    public required int PingSamples { get; init; }

    /// <summary>The delay for an averaged ping.</summary>
    public int FramesFor(int pingMilliseconds)
    {
        int steps = Math.Max(pingMilliseconds, 0) / PingStepMilliseconds;
        return steps >= MaximumFrames - MinimumFrames ? MaximumFrames : MinimumFrames + steps;
    }

    /// <exception cref="ArgumentException">A value is out of range.</exception>
    public void Validate()
    {
        if (MinimumFrames < 0 || MaximumFrames < MinimumFrames)
        {
            throw new ArgumentException(
                $"Input delay range {MinimumFrames}–{MaximumFrames} f must start at 0 or more and not run backwards.");
        }

        if (PingStepMilliseconds < 1 || PingSampleIntervalFrames < 1 || PingSamples < 1)
        {
            throw new ArgumentException("The ping step, sample interval and sample count must each be at least 1.");
        }
    }
}
