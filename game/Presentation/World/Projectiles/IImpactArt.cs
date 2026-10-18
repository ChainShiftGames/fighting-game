using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>Shows a projectile's impact for the frame of it the logic is on.</summary>
/// <remarks>Reads only; called once per displayed frame while the impact plays, so it must not allocate.</remarks>
public interface IImpactArt
{
    /// <param name="age">Logic frames since the impact's event (0 on its frame).</param>
    /// <param name="centre">Where it happened on the gameplay plane, in metres.</param>
    /// <param name="facing">The way the projectile was flying: 1 right, -1 left.</param>
    void ShowFrame(int age, ImpactKind kind, Vector3 centre, int facing);
}
