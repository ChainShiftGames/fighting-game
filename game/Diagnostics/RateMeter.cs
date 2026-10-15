namespace FightingGame.Diagnostics;

/// <summary>Counts events per second (e.g. logic frames), refreshed once per second of wall-clock time.</summary>
public sealed class RateMeter
{
    private readonly long _ticksPerSecond;
    private long _windowStart;
    private int _countInWindow;
    private bool _started;

    public RateMeter(long ticksPerSecond)
    {
        _ticksPerSecond = ticksPerSecond;
    }

    public double PerSecond { get; private set; }

    public void Count(int events, long timestamp)
    {
        if (!_started)
        {
            _started = true;
            _windowStart = timestamp;
        }

        _countInWindow += events;
        long elapsed = timestamp - _windowStart;
        if (elapsed >= _ticksPerSecond)
        {
            PerSecond = _countInWindow * (double)_ticksPerSecond / elapsed;
            _countInWindow = 0;
            _windowStart = timestamp;
        }
    }
}
