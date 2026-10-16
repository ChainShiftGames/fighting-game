namespace Sim.Rules;

/// <summary>What happens when both fighters' attacks connect on the same frame (GAP-12 stays open).</summary>
/// <remarks>Either way both fighters are treated alike; neither port wins a same-frame exchange.</remarks>
public enum TradeRule
{
    /// <summary>Both attacks land: each fighter takes the other's hit as if it had connected alone.</summary>
    Trade,

    /// <summary>Neither attack lands: both are spent (no damage, stun or hitstop) and a clash event is emitted.</summary>
    Clash,
}
