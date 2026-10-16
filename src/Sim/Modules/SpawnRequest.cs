namespace Sim.Modules;

/// <summary>Core request: spawn one of the owner's character's projectiles as an entity (ADR-0011, P1-21).</summary>
/// <remarks>
/// It appears where and as the projectile's data says, relative to the owner and facing its way, exactly as if the
/// owner's move had spawned it, and follows the same pool rules (refused when the owner's slots are full).
/// </remarks>
public struct SpawnRequest
{
    /// <summary>Index in the owner's character's projectile list (<c>CharacterData.Projectiles</c>).</summary>
    public int Projectile;
}
