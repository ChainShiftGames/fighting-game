using Sim.Characters;

namespace Sim.Modules;

/// <summary>The attack an airborne defender wants to block, for <see cref="IAirGuardHook"/>; player indices 0 and 1.</summary>
public readonly struct AirGuardCheck
{
    public AirGuardCheck(int attacker, int defender, MoveCategory attackCategory, MoveStance attackStance, bool isProjectile)
    {
        Attacker = attacker;
        Defender = defender;
        AttackCategory = attackCategory;
        AttackStance = attackStance;
        IsProjectile = isProjectile;
    }

    /// <summary>The attacking fighter, or the projectile's owner.</summary>
    public int Attacker { get; }

    public int Defender { get; }

    /// <summary>The attacking move's category (normal, special, super); <see cref="MoveCategory.None"/> for a projectile.</summary>
    public MoveCategory AttackCategory { get; }

    /// <summary>Whether the attacking move is a ground or an air move; meaningless for a projectile.</summary>
    public MoveStance AttackStance { get; }

    public bool IsProjectile { get; }
}
