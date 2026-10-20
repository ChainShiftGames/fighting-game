using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>Hands one effect light to the fighters' toon shader, which adds it to their lit side and edges.</summary>
/// <remarks>
/// Shader globals, because the fighters' materials are shared files; the toon shader's own light stays fixed (the
/// Xrd look), so this only adds colour near the effect. A radius of 0 is no light.
/// </remarks>
public sealed class EffectLighting
{
    private static readonly StringName _position = "effect_light_position";
    private static readonly StringName _color = "effect_light_color";

    private bool _lit = true;

    public void Show(Vector3 position, Color color, float radius)
    {
        _lit = true;
        RenderingServer.GlobalShaderParameterSet(_position, new Vector4(position.X, position.Y, position.Z, radius));
        RenderingServer.GlobalShaderParameterSet(_color, color);
    }

    public void Clear()
    {
        if (!_lit)
        {
            return;
        }

        _lit = false;
        RenderingServer.GlobalShaderParameterSet(_position, Vector4.Zero);
    }
}
