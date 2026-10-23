namespace FightingGame.UI.Framework;

/// <summary>Ignores presses for a short time after the menus open, so mashing at the end of a fight picks nothing.</summary>
public sealed class MenuGrace
{
    private readonly long _milliseconds;
    private long _openedAt;

    public MenuGrace(long milliseconds)
    {
        _milliseconds = milliseconds;
    }

    /// <summary>Call when the menus open from closed (not when one screen follows another).</summary>
    public void Restart(long nowMilliseconds) => _openedAt = nowMilliseconds;

    public bool HasPassed(long nowMilliseconds) => nowMilliseconds - _openedAt >= _milliseconds;
}
