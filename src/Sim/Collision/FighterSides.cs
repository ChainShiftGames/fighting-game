using Sim.State;

namespace Sim.Collision;

/// <summary>Which side of its opponent a fighter is on, for pushing them apart.</summary>
public static class FighterSides
{
    /// <summary>-1 when <paramref name="fighter"/> is left of <paramref name="opponent"/>, +1 when right, 0 when undecided.</summary>
    /// <remarks>
    /// Decided by x position. At the same x (e.g. a jump that lands on a cornered opponent) both keep the side their
    /// facing says: a fighter facing right is on the left. If their facings don't agree on that, neither side is
    /// chosen; picking one would favour a port. Swapping the arguments negates the result.
    /// </remarks>
    public static int Of(in FighterState fighter, in FighterState opponent)
    {
        int fighterX = fighter.Position.X;
        int opponentX = opponent.Position.X;
        if (fighterX != opponentX)
        {
            return fighterX < opponentX ? -1 : 1;
        }

        return fighter.Facing == -opponent.Facing ? -fighter.Facing : 0;
    }
}
