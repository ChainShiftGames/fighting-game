using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>An effect that lights what is round it on this frame; the fighters' toon shader reads it (EffectLighting).</summary>
public interface IEffectLightSource
{
    /// <summary>The light as of the last shown frame; false when it gives none.</summary>
    /// <param name="position">Where it shines from, in metres.</param>
    /// <param name="color">Its colour, brightness included.</param>
    /// <param name="radius">How far it reaches, in metres.</param>
    bool LightNow(out Vector3 position, out Color color, out float radius);
}
