namespace Sim.Characters;

/// <summary>What happens when a move's hit or throw box connects. Frame counts in frames, distance in subpixels.</summary>
public readonly record struct HitData
{
    public int Damage { get; init; }

    public int ChipDamage { get; init; }

    public Guard Guard { get; init; }

    public int Hitstun { get; init; }

    public int Blockstun { get; init; }

    public int Hitstop { get; init; }

    public int Pushback { get; init; }

    public HitReaction Reaction { get; init; }
}
