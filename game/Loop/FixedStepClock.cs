using System;

namespace FightingGame.Loop;

/// <summary>Turns elapsed wall-clock time into a count of due logic frames at a fixed rate (ADR-0004).</summary>
/// <remarks>
/// Counts in integer timer ticks scaled by the frame rate, so there is no rounding drift: over any stretch of time
/// exactly <c>seconds × FramesPerSecond</c> frames come due.
/// </remarks>
public sealed class FixedStepClock
{
    private readonly long _ticksPerSecond;
    private readonly int _framesPerSecond;
    private readonly int _maxCatchUpFrames;
    private long _lastTimestamp;
    private long _scaledRemainder;
    private bool _started;

    public FixedStepClock(LoopSettings settings, long ticksPerSecond)
    {
        _framesPerSecond = settings.FramesPerSecond;
        _maxCatchUpFrames = settings.MaxCatchUpFrames;
        _ticksPerSecond = ticksPerSecond;
    }

    /// <summary>Logic frames due since the previous call. The first call only starts the clock.</summary>
    public int Advance(long timestamp)
    {
        if (!_started)
        {
            _started = true;
            _lastTimestamp = timestamp;
            return 0;
        }

        _scaledRemainder += (timestamp - _lastTimestamp) * _framesPerSecond;
        _lastTimestamp = timestamp;

        long dueFrames = _scaledRemainder / _ticksPerSecond;
        _scaledRemainder -= dueFrames * _ticksPerSecond;
        if (dueFrames > _maxCatchUpFrames)
        {
            dueFrames = _maxCatchUpFrames;
            _scaledRemainder = 0;
        }

        return (int)Math.Max(dueFrames, 0);
    }
}
