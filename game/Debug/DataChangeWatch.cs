using System;

namespace FightingGame.Debug;

/// <summary>
/// Notices when the data on disk changed, e.g. the character editor saved, so a local or training match can start
/// over with it (P3-43). Reports a change once it has settled: a save writes several files, and the match should
/// load them all, not the first one.
/// </summary>
/// <remarks>Polls a fingerprint at a fixed interval of the game's own clock; nothing runs on another thread.</remarks>
public sealed class DataChangeWatch
{
    private readonly Func<int> _fingerprint;
    private readonly long _interval;
    private long _nextCheck;
    private int _last;
    private bool _changing;

    /// <param name="fingerprint">Reads the data's current fingerprint (<see cref="DataFolderFingerprint"/>).</param>
    /// <param name="interval">Time between checks, in timestamp ticks.</param>
    public DataChangeWatch(Func<int> fingerprint, long interval, long now)
    {
        _fingerprint = fingerprint;
        _interval = interval;
        Accept(now);
    }

    /// <summary>True once, when the data changed and then stayed the same for one interval.</summary>
    public bool HasSettledChange(long now)
    {
        if (now < _nextCheck)
        {
            return false;
        }

        _nextCheck = now + _interval;
        int current = _fingerprint();
        if (current != _last)
        {
            _last = current;
            _changing = true;
            return false;
        }

        bool settled = _changing;
        _changing = false;
        return settled;
    }

    /// <summary>Takes the data as it is now as seen, e.g. after the game itself saved and restarted the match.</summary>
    public void Accept(long now)
    {
        _last = _fingerprint();
        _changing = false;
        _nextCheck = now + _interval;
    }
}
