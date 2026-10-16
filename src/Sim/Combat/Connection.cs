using Sim.Characters;

namespace Sim.Combat;

/// <summary>One attack's contact this frame, decided: hit or block, the guarding stance and the hit's data.</summary>
public readonly record struct Connection
{
    public static Connection None => default;

    public HitOutcome Outcome { get; init; }

    /// <summary>The defender's stance when blocked; <see cref="BlockStance.None"/> on a hit.</summary>
    public BlockStance Stance { get; init; }

    /// <summary>The hit's data, with the counter hit bonus already added.</summary>
    public HitData Hit { get; init; }

    /// <summary>Whether a hit caught the defender attacking; always none on a block.</summary>
    public CounterHitKind Counter { get; init; }

    /// <summary>The attacker's move, by index in its character's move list.</summary>
    public int Move { get; init; }

    public bool IsMade => Outcome != HitOutcome.None;
}
