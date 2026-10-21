namespace FightingGame.Presentation.Hud;

/// <summary>
/// Keeps a finished combo's count on screen a little while after the defender recovers, as fighting games do, by
/// logic frames; a new combo replaces it at once.
/// </summary>
public sealed class ComboCountHold
{
    // Developer setting (presentation): logic frames a finished combo's count stays up, under a second.
    private const int HoldFrames = 50;

    private int _shown;
    private int _lastHitFrame;

    /// <param name="frame">The logic frame shown.</param>
    /// <param name="hits">The defender's combo count in that frame's state (<c>FighterState.ComboHits</c>).</param>
    /// <returns>The count to show: the live one, or the last one while it is held.</returns>
    public int Follow(int frame, int hits)
    {
        // A rollback or a new match shows an earlier frame: what it shows is the truth.
        if (hits > 0 || frame < _lastHitFrame)
        {
            _shown = hits;
            _lastHitFrame = frame;
            return hits;
        }

        return frame - _lastHitFrame <= HoldFrames ? _shown : 0;
    }
}
