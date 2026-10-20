using Sim.State;

namespace Sim.Fighters;

/// <summary>Turns a fighter toward its opponent: the one facing rule for side switches, hit reactions and recoveries.</summary>
/// <remarks>
/// Decided by x position alone, so it reads the same for both ports. At the same x nothing tells the sides apart, so
/// the fighter keeps its facing. Who turns when is up to the callers: free ground fighters every frame
/// (<c>Sim.Collision.SideSwitch</c>), a defender when a hit or block reaches it (<c>Sim.Combat</c>), and a fighter
/// whose hitstun, blockstun or wake-up ends, before it picks a buffered move (<c>Sim.Fighters.States</c>).
/// </remarks>
public static class FaceOpponent
{
    /// <summary>+1 when the opponent is to the right, -1 when left; the fighter's own facing at the same x.</summary>
    public static int FacingFor(in FighterState fighter, in FighterState opponent)
    {
        int towardOpponent = opponent.Position.X - fighter.Position.X;
        if (towardOpponent == 0)
        {
            return fighter.Facing;
        }

        return towardOpponent > 0 ? 1 : -1;
    }

    public static void Turn(ref FighterState fighter, in FighterState opponent) =>
        fighter.Facing = FacingFor(in fighter, in opponent);
}
