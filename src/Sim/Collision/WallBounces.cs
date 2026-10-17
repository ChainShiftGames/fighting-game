using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;
using Sim.State;

namespace Sim.Collision;

/// <summary>A juggled fighter touching a wall with a wall bounce armed (ADR-0022) is sent back off it, once.</summary>
/// <remarks>
/// Runs after walls and pushboxes have placed the fighters. The bounce replaces the fighter's velocity, x away from
/// the wall it touches; it only fires while the fighter moves toward that wall, so it can't fire twice in a row.
/// </remarks>
public static class WallBounces
{
    public static void Apply(ref FighterState fighter, CharacterData character, in StageWalls walls)
    {
        if (fighter.Action != FighterAction.AirHitstun || fighter.WallBounce.X <= 0)
        {
            return;
        }

        AxisAlignedBox box = Pushbox.Of(character, in fighter);
        int away = box.MinX <= walls.Left && fighter.Velocity.X < 0 ? 1
            : box.MaxX >= walls.Right && fighter.Velocity.X > 0 ? -1
            : 0;
        if (away == 0)
        {
            return;
        }

        fighter.Velocity = new IntVector2(fighter.WallBounce.X * away, fighter.WallBounce.Y);
        fighter.WallBounce = IntVector2.Zero;
    }
}
