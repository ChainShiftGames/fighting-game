namespace Sim.Modules;

/// <summary>Core request: scale what is left of one fighter's pushback slide (e.g. pushback guard, P3-15).</summary>
/// <remarks>
/// Changes <c>FighterState.Pushback</c>'s distance; it only matters while a slide is in progress, so a module asks for
/// it right after the hit or block that started one (on-hit or on-block hook). The corner rule still applies to the
/// scaled distance.
/// </remarks>
public struct PushbackScaleRequest
{
    /// <summary>The largest scale: ten times the pushback.</summary>
    public const int MaxPermille = 10_000;

    /// <summary>The distance left becomes this many thousandths of itself (2000 = twice as far, 0 = no slide).</summary>
    public int Permille;
}
