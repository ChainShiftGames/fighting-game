using Sim.Fighters.States;
using Sim.State;

namespace Sim.Collision;

/// <summary>Turns a fighter to face its opponent once the opponent has moved to its other side (crossups).</summary>
/// <remarks>
/// Only fighters on the ground and free to act turn: idle, crouching, walking, or landing. Jumping, dashing,
/// attacking or stunned fighters keep their facing until they get there, so a jump that crosses over turns the
/// jumper on landing and the opponent as soon as the jumper passes over it. At the same x nobody turns. Input is
/// read relative to facing, so the new facing applies from the next frame's input on.
/// </remarks>
public static class SideSwitch
{
    public static void Apply(ref FighterState fighter, in FighterState opponent)
    {
        int towardOpponent = opponent.Position.X - fighter.Position.X;
        if (towardOpponent != 0 && CanTurn(in fighter))
        {
            fighter.Facing = towardOpponent > 0 ? 1 : -1;
        }
    }

    private static bool CanTurn(in FighterState fighter) => fighter.IsAirborne == 0 && fighter.Action is
        FighterAction.Idle or FighterAction.Crouch or FighterAction.WalkForward or FighterAction.WalkBack
        or FighterAction.Landing;
}
