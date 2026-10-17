using Sim.Math;

namespace Sim.State;

/// <summary>One slot of the entity pool: a live projectile or module object, or an empty slot (P1-21).</summary>
/// <remarks>
/// Plain data like the rest of the state; what the entity is comes from its owner's character data by index
/// (<see cref="Projectile"/>), so the slot holds only what changes. An empty slot is all zeros.
/// </remarks>
public struct EntityState
{
    /// <summary>1 while the slot holds an entity, else 0.</summary>
    public int IsActive;

    /// <summary>The player who spawned it (0 = P1, 1 = P2); it can only touch the other one.</summary>
    public int Owner;

    /// <summary>Index in the owner's character's projectile list (<c>CharacterData.Projectiles</c>).</summary>
    public int Projectile;

    /// <summary>Frame of its life, 1 on the frame it spawned.</summary>
    public int Frame;

    public IntVector2 Position;

    public IntVector2 Velocity;

    /// <summary>The owner's facing when it spawned: +1 right, -1 left; its boxes face the same way.</summary>
    public int Facing;

    /// <summary>Clash points left against opposing projectiles.</summary>
    public int Durability;

    /// <summary>1 when a move started from its special button shortcut spawned it: its hit has the trade-off (ADR-0020).</summary>
    public int IsShortcut;
}
