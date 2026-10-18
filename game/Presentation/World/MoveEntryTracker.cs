namespace FightingGame.Presentation.World;

/// <summary>
/// Remembers, from the poses shown, which move a fighter's current move followed and on which match frame it began:
/// the game state keeps neither, and a basic move's own frame may not count (crouch is one frame long, repeated).
/// </summary>
/// <remarks>
/// Counts logic frames, never elapsed time (ARCHITECTURE.md §9). After a rollback or a rewound replay the match frame
/// goes back; the move then counts from there with no known previous move, so at worst a transition is skipped.
/// </remarks>
public sealed class MoveEntryTracker
{
    private string? _move;
    private int _startFrame;

    /// <summary>The move shown before the current one; null when unknown.</summary>
    public string? CameFrom { get; private set; }

    public void Observe(string moveId, int matchFrame)
    {
        if (moveId != _move)
        {
            CameFrom = _move;
            _move = moveId;
            _startFrame = matchFrame;
        }
        else if (matchFrame < _startFrame)
        {
            CameFrom = null;
            _startFrame = matchFrame;
        }
    }

    /// <summary>The current move's frame counted by the match clock: 1 on the frame it began.</summary>
    public int FramesInMove(int matchFrame) => matchFrame - _startFrame + 1;
}
