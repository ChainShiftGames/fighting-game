using Sim.Rules;

namespace Sim.Data.Rules;

/// <summary>The loaded <c>rules/match.json</c>: match-wide gameplay values (developer settings, ARCHITECTURE.md §6).</summary>
/// <remarks>Round start, timer and rounds to win (P1-13) live here; module order and config come later.</remarks>
public sealed class MatchRulesFile
{
    public MatchRulesFile(int roundStartDistance, int wallDistance, int maxPlayerDistance)
    {
        RoundStartDistance = roundStartDistance;
        WallDistance = wallDistance;
        MaxPlayerDistance = maxPlayerDistance;
    }

    /// <summary>Subpixels from the stage centre.</summary>
    public int RoundStartDistance { get; }

    /// <summary>Subpixels from the stage centre to each wall.</summary>
    public int WallDistance { get; }

    /// <summary>Subpixels from one fighter's outer pushbox edge to the other's, at most: the logic camera's width.</summary>
    public int MaxPlayerDistance { get; }

    /// <summary>Copies the values into the match rules, where the logic reads them.</summary>
    public void ApplyTo(ref MatchRules rules)
    {
        rules.RoundStartDistance = RoundStartDistance;
        rules.WallDistance = WallDistance;
        rules.MaxPlayerDistance = MaxPlayerDistance;
    }
}
