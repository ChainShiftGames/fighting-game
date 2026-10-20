using Sim.State;

namespace Sim.Combat;

/// <summary>Which way a hit or block sends the defender, for its pushback and launch: +1 right, -1 left.</summary>
/// <remarks>
/// A fighter's attack sends the defender away from where the attacker stands, not the way the attacker faces: a jump
/// that crosses up still faces its takeoff side (nobody turns in the air), so its facing would push the defender
/// through the attacker, and in the corner hand the wall's share back to the attacker the wrong way. At the same x
/// position can't decide, and the attacker's facing does.
/// A projectile sends the defender along its flight, wherever its owner stands, as fireballs do in fighting games:
/// one that hits from behind (its owner crossed up in the meantime) carries the defender the way it travels, toward
/// the owner. Its flight, unlike its position, can't flip when a point-blank or wide projectile overlaps the
/// defender's centre. A projectile not moving sideways sends the way it faces (its owner's facing at spawn).
/// </remarks>
public static class HitDirection
{
    /// <summary>Away from where the attacker stands; the way the attacker faces at the same x.</summary>
    public static int AwayFromAttacker(in FighterState defender, in FighterState attacker)
    {
        int fromAttacker = defender.Position.X - attacker.Position.X;
        if (fromAttacker == 0)
        {
            return attacker.Facing;
        }

        return fromAttacker > 0 ? 1 : -1;
    }

    /// <summary>The way the projectile travels; its facing when it doesn't move sideways.</summary>
    public static int AlongFlight(in EntityState projectile)
    {
        int speed = projectile.Velocity.X;
        if (speed == 0)
        {
            return projectile.Facing;
        }

        return speed > 0 ? 1 : -1;
    }
}
