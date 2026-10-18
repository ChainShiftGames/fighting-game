namespace FightingGame.VfxLab.Files;

/// <summary>The parts of a projectile effect, in the order they play (ADR-0028 §9).</summary>
public enum LabPartKind
{
    /// <summary>The move's startup, at the spawn point.</summary>
    Charge,

    /// <summary>The projectile while it flies.</summary>
    Flight,

    /// <summary>Where it ends: hit, block, clash or fizzle.</summary>
    Impact,
}
