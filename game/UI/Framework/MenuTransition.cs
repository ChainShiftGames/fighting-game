namespace FightingGame.UI.Framework;

/// <summary>
/// The fade-in of a screen that just came up. Only the look waits: the new screen takes input from its first frame.
/// </summary>
public sealed class MenuTransition
{
    private readonly long _milliseconds;
    private long _startedAt;

    /// <param name="milliseconds">Fade length; 0 turns transitions off.</param>
    public MenuTransition(long milliseconds)
    {
        _milliseconds = milliseconds;
    }

    public void Start(long nowMilliseconds) => _startedAt = nowMilliseconds;

    /// <summary>How visible the screen is, 0 to 1.</summary>
    public float Opacity(long nowMilliseconds)
    {
        if (_milliseconds <= 0)
        {
            return 1f;
        }

        long elapsed = nowMilliseconds - _startedAt;
        return elapsed >= _milliseconds ? 1f : elapsed < 0 ? 0f : (float)elapsed / _milliseconds;
    }
}
