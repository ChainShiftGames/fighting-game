using Sim.Fighters;
using Sim.Rules;

namespace Sim.Data.Rules;

/// <summary>The loaded fighter flag list (ADR-0023): names in bit order, and which flags a hit takes away.</summary>
/// <remarks>Developer settings: the names are in the data hash, the cleared-when-hit bits in the match rules.</remarks>
public sealed class FighterFlagsFile
{
    public FighterFlagsFile(FighterFlagNames names, int clearedWhenHit)
    {
        Names = names;
        ClearedWhenHit = clearedWhenHit;
    }

    public FighterFlagNames Names { get; }

    /// <summary>One bit per flag marked <c>clearedWhenHit</c>.</summary>
    public int ClearedWhenHit { get; }

    public void ApplyTo(ref MatchRules rules) => rules.FlagsClearedWhenHit = ClearedWhenHit;
}
