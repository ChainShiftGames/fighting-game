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

    /// <summary>Fighter flags that must all be set for the move to start (ADR-0023), e.g. a stance; 0 = none.</summary>
    public int RequiredFlags { get; init; }

    /// <summary>Fighter flags that must all be clear for the move to start, e.g. a burnout lock; 0 = none.</summary>
    public int ForbiddenFlags { get; init; }

    /// <summary>Fighter flags the move sets as it starts (after <see cref="ClearsFlags"/>), e.g. entering a stance.</summary>
    public int SetsFlags { get; init; }

    /// <summary>Fighter flags the move clears as it starts, e.g. leaving a stance.</summary>
    public int ClearsFlags { get; init; }

    /// <summary>The meter channel <see cref="CostAmount"/> is paid from (ADR-0023); 0 is the main meter.</summary>
    public int CostChannel { get; init; }

    /// <summary>What starting the move costs; a fighter without that much can't start it. 0 = free.</summary>
    public int CostAmount { get; init; }

    /// <summary>1: the move never starts from neutral or by category, only from a cancel window naming it (a rekka).</summary>
    /// <remarks>An int, not a bool: hashed character structs hold only four-byte fields.</remarks>
    public int IsFollowUpOnly { get; init; }

    /// <summary>The fighter can't be hit or thrown on <paramref name="frame"/> of this move.</summary>
    public bool IsInvulnerableOn(int frame) =>
        InvulnerableFirstFrame != 0 && frame >= InvulnerableFirstFrame && frame <= InvulnerableLastFrame;
}
