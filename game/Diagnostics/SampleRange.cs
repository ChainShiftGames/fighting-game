using System;

namespace FightingGame.Diagnostics;

/// <summary>Lowest, highest and average of whole-number samples since the start (e.g. ping readings).</summary>
public sealed class SampleRange
{
    private long _total;

    public int Count { get; private set; }

    public int Min { get; private set; }

    public int Max { get; private set; }

    public double Average => Count == 0 ? 0 : (double)_total / Count;

    public void Add(int sample)
    {
        Min = Count == 0 ? sample : Math.Min(Min, sample);
        Max = Count == 0 ? sample : Math.Max(Max, sample);
        _total += sample;
        Count++;
    }
}
