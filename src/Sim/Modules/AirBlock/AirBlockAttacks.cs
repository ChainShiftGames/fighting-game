using Sim.Characters;

namespace Sim.Modules.AirBlock;

/// <summary>The attack kinds <see cref="AirBlockConfig.UnblockableAttacks"/> can rule out, one bit each.</summary>
/// <remarks>Integers rather than a flags enum, because module configs hold only ints.</remarks>
public static class AirBlockAttacks
{
    public const int GroundNormals = 1 << 0;
    public const int AirNormals = 1 << 1;

    /// <summary>Special moves, ground or air.</summary>
    public const int Specials = 1 << 2;

    public const int Supers = 1 << 3;
    public const int Projectiles = 1 << 4;

    /// <summary>The kind bit of the attack an airborne fighter wants to block.</summary>
    public static int Of(in AirGuardCheck check)
    {
        if (check.IsProjectile)
        {
            return Projectiles;
        }

        return check.AttackCategory switch
        {
            MoveCategory.Normal => check.AttackStance == MoveStance.Air ? AirNormals : GroundNormals,
            MoveCategory.Special => Specials,
            MoveCategory.Super => Supers,
            _ => 0,
        };
    }
}
