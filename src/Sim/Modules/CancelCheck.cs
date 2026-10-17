using Sim.Characters;

namespace Sim.Modules;

/// <summary>A cancel a fighter's data cancel window offers, for <see cref="ICancelWindowHook"/>.</summary>
public readonly struct CancelCheck
{
    public CancelCheck(
        int fighter,
        MoveCategory fromCategory,
        MoveStance fromStance,
        CancelOn outcome,
        MoveCategory intoCategory,
        bool isJump)
    {
        Fighter = fighter;
        FromCategory = fromCategory;
        FromStance = fromStance;
        Outcome = outcome;
        IntoCategory = intoCategory;
        IsJump = isJump;
    }

    /// <summary>The fighter cancelling: 0 = P1, 1 = P2.</summary>
    public int Fighter { get; }

    /// <summary>The category of the move being cancelled (normal, special, super, throw).</summary>
    public MoveCategory FromCategory { get; }

    public MoveStance FromStance { get; }

    /// <summary>What the cancelled move has done so far (hit, block, whiff), which opened the window.</summary>
    public CancelOn Outcome { get; }

    /// <summary>The category of the move cancelled into; <see cref="MoveCategory.None"/> for a jump cancel.</summary>
    public MoveCategory IntoCategory { get; }

    /// <summary>A jump cancel: into a prejump on the ground, a double jump in the air.</summary>
    public bool IsJump { get; }
}
