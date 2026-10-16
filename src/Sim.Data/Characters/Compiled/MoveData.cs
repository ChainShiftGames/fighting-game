namespace Sim.Data.Characters.Compiled;

/// <summary>
/// One move in compact form. Boxes, cancels and hits are ranges into the character's flat arrays, so the data
/// holds no object references.
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
}
