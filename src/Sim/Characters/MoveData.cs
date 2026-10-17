namespace Sim.Characters;

/// <summary>
/// One move in compact form. Boxes, cancels, hits, velocities and projectiles are ranges into the character's flat
/// arrays, so the data holds no object references.
/// </summary>
public readonly record struct MoveData
{
    public MoveCategory Category { get; init; }

    public MoveStance Stance { get; init; }

    /// <summary>Numpad notation read as a number (236, 623, 5 for no motion); 0 for basic moves.</summary>
    public int Motion { get; init; }

    /// <summary>The button's bit in the input word (ADR-0010); 0 for basic moves.</summary>
    public int ButtonMask { get; init; }

    /// <summary>
    /// The special button shortcut (ADR-0020): a numpad direction 1–9 held as the special button is pressed starts
    /// this move instead of its motion and button; 0 when the move has none.
    /// </summary>
    public int ShortcutDirection { get; init; }

    /// <summary>Length of the move; basic moves loop over it.</summary>
    public int TotalFrames { get; init; }

    /// <summary>First active frame (1-based); 0 when the move has no active frames.</summary>
    public int FirstActiveFrame { get; init; }

    public int LastActiveFrame { get; init; }

    public int BoxStart { get; init; }

    public int BoxCount { get; init; }

    public int CancelStart { get; init; }

    public int CancelCount { get; init; }

    public int HitStart { get; init; }

    public int HitCount { get; init; }

    public int VelocityStart { get; init; }

    public int VelocityCount { get; init; }

    /// <summary>The projectiles the move spawns, in data order: a range in <see cref="CharacterData.Projectiles"/>.</summary>
    public int ProjectileStart { get; init; }

    public int ProjectileCount { get; init; }

    /// <summary>First frame (1-based) the fighter can't be hit or thrown; 0 when the move has none.</summary>
    public int InvulnerableFirstFrame { get; init; }

    public int InvulnerableLastFrame { get; init; }

    /// <summary>
    /// For a move with throw boxes: where the thrown fighter lands relative to the thrower when the throw completes,
    /// in subpixels along the thrower's facing (negative: behind it, a back throw). 0 for other moves.
    /// </summary>
    public int ThrowReleaseX { get; init; }

    /// <summary>The fighter can't be hit or thrown on <paramref name="frame"/> of this move.</summary>
    public bool IsInvulnerableOn(int frame) =>
        InvulnerableFirstFrame != 0 && frame >= InvulnerableFirstFrame && frame <= InvulnerableLastFrame;
}
