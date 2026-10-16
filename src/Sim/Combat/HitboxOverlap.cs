using Sim.Characters;
using Sim.Collision;
using Sim.Math;
using Sim.State;

namespace Sim.Combat;

/// <summary>Finds where one fighter's hitboxes touch the other fighter's hurtboxes this frame.</summary>
public static class HitboxOverlap
{
    /// <summary>True when a hitbox of the attacker overlaps a hurtbox of the defender.</summary>
    /// <param name="point">The centre of the first overlapping pair (data order), e.g. for the hit spark.</param>
    public static bool TryFind(
        CharacterData attackerCharacter,
        in FighterState attacker,
        CharacterData defenderCharacter,
        in FighterState defender,
        out IntVector2 point)
    {
        foreach (AxisAlignedBox hitbox in new ActiveBoxes(attackerCharacter, in attacker, BoxKind.Hit))
        {
            foreach (AxisAlignedBox hurtbox in new ActiveBoxes(defenderCharacter, in defender, BoxKind.Hurt))
            {
                if (hitbox.Overlaps(hurtbox))
                {
                    point = CentreOfOverlap(hitbox, hurtbox);
                    return true;
                }
            }
        }

        point = IntVector2.Zero;
        return false;
    }

    // Halving rounds toward zero, which is mirror-symmetric: the same exchange on the other side of the stage gives
    // the mirrored point.
    private static IntVector2 CentreOfOverlap(AxisAlignedBox first, AxisAlignedBox second)
    {
        int minX = first.MinX > second.MinX ? first.MinX : second.MinX;
        int maxX = first.MaxX < second.MaxX ? first.MaxX : second.MaxX;
        int minY = first.MinY > second.MinY ? first.MinY : second.MinY;
        int maxY = first.MaxY < second.MaxY ? first.MaxY : second.MaxY;
        return new IntVector2((minX + maxX) / 2, (minY + maxY) / 2);
    }
}
