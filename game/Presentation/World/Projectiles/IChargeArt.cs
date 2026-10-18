using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>Shows a projectile move's startup charge at the point the projectile will spawn from.</summary>
/// <remarks>Reads only; called once per displayed frame during the startup, so it must not allocate.</remarks>
public interface IChargeArt
{
    /// <param name="moveFrame">The move's frame (from 1): the animation frame, never elapsed time.</param>
    /// <param name="spawnFrame">The move frame the projectile appears on; the charge ends the frame before.</param>
    /// <param name="centre">Where the projectile's hitbox centre will be on its first frame, in metres.</param>
    /// <param name="facing">1 facing right, -1 facing left.</param>
    void ShowFrame(int moveFrame, int spawnFrame, Vector3 centre, int facing);
}
