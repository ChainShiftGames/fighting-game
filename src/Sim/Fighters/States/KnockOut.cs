using Sim.State;

namespace Sim.Fighters.States;

/// <summary>When a fighter is KO'd: the one definition shared by round logic, hit reactions and the state machine.</summary>
public static class KnockOut
{
    public static bool IsKnockedOut(in FighterState fighter) => fighter.Health <= 0;
}
