using System;

namespace Benchmarks.Drivers;

/// <summary>Whole match-driver frames, Backdash included, timed one at a time by <see cref="FrameTimer"/>.</summary>
internal interface IDriverBenchmark : IDisposable
{
    string Name { get; }

    /// <summary>Match drivers running in this process; allocations are shared out over them.</summary>
    int DriversInProcess { get; }

    /// <summary>Untimed work before each timed frame, e.g. the other peer's frame and pacing.</summary>
    void BetweenFrames();

    /// <summary>One timed frame of the measured driver. False when the driver couldn't advance (it waited).</summary>
    bool AdvanceFrame();
}
