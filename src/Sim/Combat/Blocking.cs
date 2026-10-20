using Sim.Characters;
using Sim.Fighters;
using Sim.Fighters.States;
using Sim.Input;
using Sim.State;

namespace Sim.Combat;

/// <summary>Stand and crouch block: whether a defender guards, how, and whether that stops a given attack.</summary>
/// <remarks>
/// A defender guards on the ground while free to act (standing, walking, crouching) or already in blockstun, by
/// holding back this frame: away from the attacker, or away from where it faces when both stand at the same x. The
/// input decides the stance, not the current action, so a defender in blockstun can switch between high and low.
/// A projectile is blocked the same way, against its owner: "back" is always away from the opponent, wherever the
/// projectile comes from. Airborne fighters guard only through a module (<see cref="Guarding"/>): in the air a fighter
/// holding back while jumping or already in air blockstun asks to block, and an air block stops anything blockable.
/// </remarks>
public static class Blocking
{
    public static BlockStance StanceOf(in FighterState defender, in FighterState attacker, InputWord input)
    {
        if (defender.IsAirborne != 0 || !CanGuardFrom(defender.Action))
        {
            return BlockStance.None;
        }

        int direction = NumpadDirection.Of(input, TowardAttacker(in defender, in attacker));
        if (!NumpadDirection.IsBack(direction))
        {
            return BlockStance.None;
        }

        return NumpadDirection.IsDown(direction) ? BlockStance.Crouching : BlockStance.Standing;
    }

    /// <summary>
    /// An airborne defender free to guard in the air (jumping or in air blockstun) holds back: it asks to air block.
    /// </summary>
    public static bool IsAirGuardInput(in FighterState defender, in FighterState attacker, InputWord input) =>
        defender.IsAirborne != 0 &&
        defender.Action is FighterAction.Jump or FighterAction.AirBlockstun &&
        NumpadDirection.IsBack(NumpadDirection.Of(input, TowardAttacker(in defender, in attacker)));

    /// <summary>True when <paramref name="stance"/> blocks an attack with <paramref name="guard"/>.</summary>
    public static bool Stops(BlockStance stance, Guard guard) => guard switch
    {
        Guard.Mid => stance != BlockStance.None,
        Guard.Low => stance is BlockStance.Crouching or BlockStance.Air,
        Guard.Overhead => stance is BlockStance.Standing or BlockStance.Air,
        _ => false,
    };

    private static bool CanGuardFrom(FighterAction action) => action is FighterAction.Idle or FighterAction.Crouch
        or FighterAction.WalkForward or FighterAction.WalkBack or FighterAction.Blockstun
        or FighterAction.CrouchBlockstun;

    /// <summary>
    /// +1 when the attacker is to the right, -1 when left (at the same x, the defender's facing): the facing to read
    /// the defender's input with, so that back (<see cref="NumpadDirection.IsBack"/>) is away from the attacker.
    /// </summary>
    public static int TowardAttacker(in FighterState defender, in FighterState attacker) =>
        FaceOpponent.FacingFor(in defender, in attacker);
}
