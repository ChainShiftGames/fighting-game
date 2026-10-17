using Sim.Math;

namespace Sim.Characters;

/// <summary>What happens when a move's hit or throw box connects. Frame counts in frames, distance in subpixels.</summary>
/// <remarks>
/// A move's hits each cover their own move frames (ADR-0022); launch, bounce, knockdown and OTG fields are optional
/// and default to the plain v1 behaviour. Velocities are in subpixels per frame, x away from the attacker, y up.
/// </remarks>
public readonly record struct HitData
{
    /// <summary>First move frame (1-based) this hit's boxes are active on; projectile hits use 0.</summary>
    public int FirstFrame { get; init; }

    public int LastFrame { get; init; }

    public int Damage { get; init; }

    public int ChipDamage { get; init; }

    public Guard Guard { get; init; }

    public int Hitstun { get; init; }

    public int Blockstun { get; init; }

    public int Hitstop { get; init; }

    public int Pushback { get; init; }

    public HitReaction Reaction { get; init; }

    /// <summary>This hit's own counter hit bonus; when not set, the match rules' default applies.</summary>
    public CounterBonus CounterHit { get; init; }

    /// <summary>This hit's own punish counter bonus; when not set, the match rules' default applies.</summary>
    public CounterBonus PunishCounter { get; init; }

    /// <summary>1 when the hit launches: the defender takes <see cref="Launch"/> and is airborne in air hitstun.</summary>
    public int HasLaunch { get; init; }

    public IntVector2 Launch { get; init; }

    /// <summary>1 when an already airborne defender takes <see cref="AirLaunch"/> instead of <see cref="Launch"/>.</summary>
    public int HasAirLaunch { get; init; }

    public IntVector2 AirLaunch { get; init; }

    /// <summary>Upward speed of the bounce on the juggle's first landing; 0 = no ground bounce.</summary>
    public int GroundBounce { get; init; }

    /// <summary>Speed off the wall on the juggle's first wall touch (x away from the wall); x 0 = no wall bounce.</summary>
    public IntVector2 WallBounce { get; init; }

    /// <summary>How long the knockdown this hit causes lasts; 0 = the knockdown move's own length.</summary>
    public int KnockdownFrames { get; init; }

    /// <summary>1 when the hit can connect with a knocked-down defender (OTG).</summary>
    public int Otg { get; init; }

    /// <summary>This hit covers <paramref name="frame"/> of its move.</summary>
    public bool Covers(int frame) => frame >= FirstFrame && frame <= LastFrame;
}
