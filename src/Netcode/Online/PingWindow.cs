using System;

namespace Netcode.Online;

/// <summary>The newest ping readings and their median, which one spike or one too-low reading can't move.</summary>
/// <remarks>
/// Single Backdash readings can be far off the real round trip in both directions (ADR-0014 problem 4), so the
/// delay follows the median of a window, never one reading. Fixed buffers: no allocation after construction.
/// </remarks>
public sealed class PingWindow
{
    private readonly int[] _readings;
    private readonly int[] _sorted;
    private int _next;
    private int _count;

    /// <param name="size">Readings kept: the newest this many.</param>
    public PingWindow(int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        _readings = new int[size];
        _sorted = new int[size];
    }

    public int Count => _count;

    public void Add(int pingMilliseconds)
    {
        _readings[_next] = pingMilliseconds;
        _next = (_next + 1) % _readings.Length;
        _count = Math.Min(_count + 1, _readings.Length);
    }

    /// <summary>The median reading (the lower middle one for an even count); false while there are none.</summary>
    public bool TryGetMedian(out int pingMilliseconds)
    {
        if (_count == 0)
        {
            pingMilliseconds = 0;
            return false;
        }

        Span<int> sorted = _sorted.AsSpan(0, _count);
        _readings.AsSpan(0, _count).CopyTo(sorted);
        sorted.Sort();
        pingMilliseconds = sorted[(_count - 1) / 2];
        return true;
    }
}
