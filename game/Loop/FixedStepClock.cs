using System;

namespace FightingGame.Loop;

/// <summary>Turns elapsed wall-clock time into a count of due logic frames at a fixed rate (ADR-0004).</summary>
/// <remarks>
/// Counts in integer timer ticks scaled by the frame rate, so there is no rounding drift: over any stretch of time
/// <c>seconds × FramesPerSecond</c> frames come due, give or take one.
/// <para>
/// Logic-frame boundaries start halfway between frame callbacks. With a display at about the logic rate, a boundary
/// right on a callback would let ordinary timing jitter flip the count between 0 and 2 per displayed frame: stutter
/// and a varying frame of input delay. Mid-frame, the jitter has half a frame of room. When the display drifts
/// against the logic rate and a boundary does reach a callback, the jitter tolerance holds the count at one until the
/// drift is clearly past, so it becomes one clean extra or skipped frame instead of a burst of them.
/// </para>
/// <para>
/// This only decides when frames run, never what they compute; input is still read immediately before each one.
/// </para>
/// </remarks>
public sealed class FixedStepClock
{
    private const int PermillePerFrame = 1000;

    private readonly int _framesPerSecond;
    private readonly int _maxCatchUpFrames;

    // Lengths in scaled ticks (timer ticks × frames per second), the unit of _scaledRemainder.
    private readonly long _frameLength;
    private readonly long _halfFrame;
    private readonly long _jitterTolerance;

    // Callbacks further apart than this aren't "about one logic frame" apart (a 30 Hz display, a hitch), so the
    // tolerance stays out of the way and every due frame runs at once.
    private readonly long _longestOneFrameInterval;

    private long _lastTimestamp;
    private long _scaledRemainder;
    private bool _started;

    public FixedStepClock(LoopSettings settings, long ticksPerSecond)
    {
        if (settings.JitterTolerancePermille is < 0 or >= PermillePerFrame / 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                settings.JitterTolerancePermille,
                "The jitter tolerance must be at least 0 and below half a logic frame (500 permille).");
        }

        _framesPerSecond = settings.FramesPerSecond;
        _maxCatchUpFrames = settings.MaxCatchUpFrames;
        _frameLength = ticksPerSecond;
        _halfFrame = _frameLength / 2;
        _jitterTolerance = _frameLength * settings.JitterTolerancePermille / PermillePerFrame;
        _longestOneFrameInterval = _frameLength + _halfFrame;
    }

    /// <summary>Logic frames due since the previous call. The first call only starts the clock.</summary>
    public int Advance(long timestamp)
    {
        if (!_started)
        {
            _started = true;
            _lastTimestamp = timestamp;
            _scaledRemainder = _halfFrame;
            return 0;
        }

        long interval = (timestamp - _lastTimestamp) * _framesPerSecond;
        _lastTimestamp = timestamp;
        _scaledRemainder += interval;

        long dueFrames = Math.Max(_scaledRemainder / _frameLength, 0);
        _scaledRemainder -= dueFrames * _frameLength;
        if (dueFrames > _maxCatchUpFrames)
        {
            // The dropped time takes the phase with it: restart mid-frame so the jitter has room again.
            _scaledRemainder = _halfFrame;
            return _maxCatchUpFrames;
        }

        return (int)KeepOneFrameWithinTolerance(dueFrames, interval);
    }

    // A boundary within the tolerance of this callback counts as being on the side that runs one frame. The remainder
    // then stays within the tolerance of [0, one frame), so no time is gained or lost, only shifted by that much.
    private long KeepOneFrameWithinTolerance(long dueFrames, long interval)
    {
        if (interval >= _longestOneFrameInterval)
        {
            return dueFrames;
        }

        if (dueFrames == 0 && _scaledRemainder >= _frameLength - _jitterTolerance)
        {
            _scaledRemainder -= _frameLength;
            return 1;
        }

        if (dueFrames == 2 && _scaledRemainder < _jitterTolerance)
        {
            _scaledRemainder += _frameLength;
            return 1;
        }

        return dueFrames;
    }
}
