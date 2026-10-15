using System;

namespace FightingGame.Diagnostics;

/// <summary>
/// Prototype input-latency counter (ADR-0004 §6): time from reading the input to the frame that shows it being
/// drawn. Ends at "drawn", not "on screen": the wait for vsync and the monitor's own delay come on top.
/// </summary>
public sealed class InputLatencyCounter
{
    private const long NoRead = -1;

    private readonly long[] _samples;
    private readonly long _ticksPerSecond;
    private long _oldestUnshownRead = NoRead;
    private int _sampleCount;
    private int _nextSample;

    /// <param name="windowSize">Recent frames the average and maximum cover (developer setting).</param>
    /// <param name="ticksPerSecond">Resolution of the timestamps passed in.</param>
    public InputLatencyCounter(int windowSize, long ticksPerSecond)
    {
        _samples = new long[windowSize];
        _ticksPerSecond = ticksPerSecond;
    }

    public double AverageMilliseconds { get; private set; }

    public double MaxMilliseconds { get; private set; }

    // When several logic frames run before one draw, the first frame's input waited longest: that one is counted.
    public void InputRead(long timestamp)
    {
        if (_oldestUnshownRead == NoRead)
        {
            _oldestUnshownRead = timestamp;
        }
    }

    public void FrameDrawn(long timestamp)
    {
        if (_oldestUnshownRead == NoRead)
        {
            return;
        }

        _samples[_nextSample] = timestamp - _oldestUnshownRead;
        _oldestUnshownRead = NoRead;
        _nextSample = (_nextSample + 1) % _samples.Length;
        _sampleCount = Math.Min(_sampleCount + 1, _samples.Length);
        Summarise();
    }

    private void Summarise()
    {
        long total = 0;
        long max = 0;
        for (int i = 0; i < _sampleCount; i++)
        {
            total += _samples[i];
            max = Math.Max(max, _samples[i]);
        }

        AverageMilliseconds = ToMilliseconds(total) / _sampleCount;
        MaxMilliseconds = ToMilliseconds(max);
    }

    private double ToMilliseconds(long ticks) => ticks * 1000.0 / _ticksPerSecond;
}
