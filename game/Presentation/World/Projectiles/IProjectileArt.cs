using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>Shows one projectile in the 3D scene for the frame of its life the logic is on.</summary>
/// <remarks>Reads only; called once per displayed frame while the projectile exists, so it must not allocate.</remarks>
public interface IProjectileArt
{
    /// <param name="frame">Frame of its life, 1 on the frame it spawned: the animation frame, never elapsed time.</param>
    /// <param name="centre">The centre of its hitbox on the gameplay plane, in metres.</param>
    /// <param name="facing">1 flying right, -1 flying left.</param>
    void ShowFrame(int frame, Vector3 centre, int facing);
}
