namespace Sim.Characters;

/// <summary>
/// A cancel window: during these frames, after the given outcomes, the move can be cancelled into the listed
/// moves or any move of the listed categories, and into a jump if <see cref="JumpCancel"/> is set.
/// </summary>
/// <remarks>
/// Whether the match lets a fighter take these cancels is up to the modules (<c>ICancelWindowHook</c>), except for a
/// follow-up window (<see cref="IsFollowUp"/>), which is part of the move itself.
/// </remarks>
public readonly record struct CancelData
{
    public int FirstFrame { get; init; }

    public int LastFrame { get; init; }

    public CancelOn On { get; init; }

    public MoveCategory IntoCategories { get; init; }

    /// <summary>Range in <see cref="CharacterData.CancelTargets"/> holding the target move indexes.</summary>
    public int TargetStart { get; init; }

    public int TargetCount { get; init; }

    /// <summary>1 when the window also allows a jump cancel (a prejump, or a double jump in the air), else 0.</summary>
    /// <remarks>An int, not a bool: hashed character structs hold only four-byte fields.</remarks>
    public int JumpCancel { get; init; }

    /// <summary>1: a follow-up window (rekka, target combo) the core always allows, without asking the modules (ADR-0023).</summary>
    public int IsFollowUp { get; init; }
}
