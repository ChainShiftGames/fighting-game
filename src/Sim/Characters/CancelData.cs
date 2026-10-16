namespace Sim.Characters;

/// <summary>
/// A cancel window: during these frames, after the given outcomes, the move can be cancelled into the listed
/// moves or any move of the listed categories.
/// </summary>
public readonly record struct CancelData
{
    public int FirstFrame { get; init; }

    public int LastFrame { get; init; }

    public CancelOn On { get; init; }

    public MoveCategory IntoCategories { get; init; }

    /// <summary>Range in <see cref="CharacterData.CancelTargets"/> holding the target move indexes.</summary>
    public int TargetStart { get; init; }

    public int TargetCount { get; init; }
}
