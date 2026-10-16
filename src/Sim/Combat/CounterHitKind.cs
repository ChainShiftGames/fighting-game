namespace Sim.Combat;

/// <summary>Whether a hit caught the defender in an attack (ADR-0017), and in which part of it.</summary>
public enum CounterHitKind
{
    /// <summary>A plain hit: the defender wasn't attacking.</summary>
    None,

    /// <summary>The defender was in an attack's startup or active frames.</summary>
    CounterHit,

    /// <summary>The defender was in an attack's recovery.</summary>
    PunishCounter,
}
