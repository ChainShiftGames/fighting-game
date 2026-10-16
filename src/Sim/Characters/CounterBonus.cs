namespace Sim.Characters;

/// <summary>What a counter hit or punish counter adds to a hit (ADR-0017). All zero: nothing, only the event.</summary>
/// <remarks>Only 4-byte fields, so hit data and the match rules hash byte for byte.</remarks>
public readonly record struct CounterBonus
{
    /// <summary>
    /// In hit data: 1 when the hit has its own bonus, 0 when the match rules' default applies. The match rules'
    /// defaults ignore it.
    /// </summary>
    public int IsSet { get; init; }

    /// <summary>Frames added to the hit's hitstun.</summary>
    public int ExtraHitstun { get; init; }

    /// <summary>Frames added to the hit's hitstop.</summary>
    public int ExtraHitstop { get; init; }

    /// <summary>Damage added as a percentage of the hit's damage, rounded down (20 = 120 % damage).</summary>
    public int ExtraDamagePercent { get; init; }
}
