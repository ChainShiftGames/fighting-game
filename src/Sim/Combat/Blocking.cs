using Sim.Characters;
using Sim.Collision;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Math;
using Sim.State;

namespace Sim.Combat;

/// <summary>Stand and crouch block: whether a defender guards, how, and whether that stops a given attack.</summary>
/// <remarks>
/// A defender guards on the ground while free to act (standing, walking, crouching) or already in blockstun, by
/// holding back this frame: away from the attacker, or away from where it faces when both stand at the same x. The
/// input decides the stance, not the current action, so a defender in blockstun can switch between high and low.
/// Airborne fighters don't block yet (air block rules are tunable, CURRENT_STATE §3 Defense). A projectile is blocked
/// the same way, holding away from the projectile itself (P1-21), or from where the defender faces at the same x.
/// </remarks>
public static class Blocking
{
    public static BlockStance StanceOf(in FighterState defender, in FighterState attacker, InputWord input) =>
        StanceToward(in defender, TowardAttacker(in defender, in attacker), input);

    /// <summary>The defender's stance against a projectile at <paramref name="projectileX"/>.</summary>
    public static BlockStance StanceAgainstProjectile(in FighterState defender, int projectileX, InputWord input)
    {
        int side = IntMath.Sign(projectileX - defender.Position.X);
        return StanceToward(in defender, side == 0 ? defender.Facing : side, input);
    }

    // towardAttack: +1 when the attack comes from the right; the input word reads "back" as away from it.
    private static BlockStance StanceToward(in FighterState defender, int towardAttack, InputWord input)
    {
        if (defender.IsAirborne != 0 || !CanGuardFrom(defender.Action))
        {
            return BlockStance.None;
        }

        int direction = NumpadDirection.Of(input, towardAttack);
        if (!NumpadDirection.IsBack(direction))
        {
            return BlockStance.None;
        }

        return NumpadDirection.IsDown(direction) ? BlockStance.Crouching : BlockStance.Standing;
    }

    /// <summary>True when <paramref name="stance"/> blocks an attack with <paramref name="guard"/>.</summary>
    public static bool Stops(BlockStance stance, Guard guard) => guard switch
    {
        Guard.Mid => stance != BlockStance.None,
        Guard.Low => stance == BlockStance.Crouching,
        Guard.Overhead => stance == BlockStance.Standing,
        _ => false,
    };

    private static bool CanGuardFrom(FighterAction action) => action is FighterAction.Idle or FighterAction.Crouch
        or FighterAction.WalkForward or FighterAction.WalkBack or FighterAction.Blockstun
        or FighterAction.CrouchBlockstun;

    // +1 when the attacker is to the right; the input word reads "back" as away from it.
    private static int TowardAttacker(in FighterState defender, in FighterState attacker)
    {
        int side = FighterSides.Of(in defender, in attacker);
        return side == 0 ? defender.Facing : -side;
    }
}
