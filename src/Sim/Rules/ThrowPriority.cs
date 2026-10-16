namespace Sim.Rules;

/// <summary>Which wins when a throw and the thrown fighter's strike connect on the same frame (GAP-12).</summary>
/// <remarks>Either way both fighters are treated alike; neither port wins.</remarks>
public enum ThrowPriority
{
    /// <summary>The throw grabs and the defender's strike is cancelled (standard 2D practice).</summary>
    Throw,

    /// <summary>The strike lands and the throw doesn't grab.</summary>
    Strike,
}
