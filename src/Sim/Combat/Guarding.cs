using Sim.Input;
using Sim.Modules;
using Sim.State;

namespace Sim.Combat;

/// <summary>How a defender guards an attack: stand or crouch block on the ground, air block where a module allows.</summary>
/// <remarks>
/// On the ground <see cref="Blocking"/> decides alone. In the air the core only knows that the defender asks to
/// block (<see cref="Blocking.IsAirGuardInput"/>); whether that attack can be blocked in the air is up to the
/// modules (<see cref="IAirGuardHook"/>), so without an air block module airborne fighters never block. Shared by
/// fighter attacks and projectiles. Built once at match start; asking allocates nothing.
/// </remarks>
public sealed class Guarding
{
    private readonly ModuleLineup _modules;

    public Guarding(ModuleLineup modules)
    {
        _modules = modules;
    }

    /// <param name="state">The match, for the modules' read-only view; fighters are passed separately.</param>
    /// <param name="defender">The defender as hit resolution sees it.</param>
    /// <param name="attacker">The attacker (a projectile's owner).</param>
    /// <param name="input">The defender's input this frame.</param>
    /// <param name="check">The attack, for the modules.</param>
    public BlockStance StanceOf(
        ref MatchState state, in FighterState defender, in FighterState attacker, InputWord input, in AirGuardCheck check)
    {
        if (defender.IsAirborne == 0)
        {
            return Blocking.StanceOf(in defender, in attacker, input);
        }

        bool isAirBlocked = Blocking.IsAirGuardInput(in defender, in attacker, input) &&
            _modules.AllowsAirGuard(ref state, in check);
        return isAirBlocked ? BlockStance.Air : BlockStance.None;
    }
}
