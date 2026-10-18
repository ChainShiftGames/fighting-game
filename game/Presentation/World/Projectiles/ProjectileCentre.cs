using Godot;
using Sim.Characters;
using Sim.Math;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>Where a projectile is drawn: the centre of the hitbox it has on a frame, mirrored with its facing.</summary>
public static class ProjectileCentre
{
    /// <param name="position">The projectile's logic position, in subpixels.</param>
    /// <param name="frame">Frame of its life, 1 on the frame it spawns.</param>
    /// <returns>The hitbox centre in metres, or the position itself on a frame without a hitbox.</returns>
    public static Vector3 Of(CharacterData owner, in ProjectileData projectile, IntVector2 position, int facing, int frame)
    {
        foreach (BoxData box in owner.BoxesOf(in projectile))
        {
            if (frame >= box.FirstFrame && frame <= box.LastFrame)
            {
                AxisAlignedBox area = facing < 0 ? box.Area.MirrorX() : box.Area;
                return WorldScale.CentreOf(area.Translate(position));
            }
        }

        return WorldScale.ToWorld(position);
    }

    /// <summary>Where a projectile spawned by an owner at <paramref name="ownerPosition"/> appears, in subpixels.</summary>
    public static IntVector2 SpawnPosition(in ProjectileData projectile, IntVector2 ownerPosition, int facing) =>
        new(ownerPosition.X + (projectile.Offset.X * facing), ownerPosition.Y + projectile.Offset.Y);
}
