using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>A camera-facing quad drawn by one effect shader from <c>Assets/Effects</c>, with the effect clock it takes.</summary>
/// <remarks>The shader reads <c>t</c> (seconds from the logic frame) and <c>spin</c>, so it never runs on elapsed time.</remarks>
public partial class ShaderCard : MeshInstance3D
{
    private static readonly StringName _time = "t";
    private static readonly StringName _spin = "spin";
    private static readonly StringName _cardSize = "card_size";

    private readonly ShaderMaterial _material = new();

    // Godot needs a parameterless constructor for nodes; the shader and size come in once, before the first frame.
    public void Build(string shader, Vector2 size, Vector2 centre) =>
        Build(GD.Load<Shader>($"res://Assets/Effects/{shader}.gdshader"), size, centre);

    /// <summary>Draws <paramref name="shader"/>; call again to change the card's size (the VFX Lab's sliders do).</summary>
    public void Build(Shader shader, Vector2 size, Vector2 centre)
    {
        _material.Shader = shader;
        _material.SetShaderParameter(_cardSize, size);
        Mesh = new QuadMesh { Size = size };
        MaterialOverride = _material;
        CastShadow = ShadowCastingSetting.Off;
        Position = new Vector3(centre.X, centre.Y, 0f);
    }

    public void SetParameter(StringName name, Variant value) => _material.SetShaderParameter(name, value);

    /// <summary>Sets the clock for <paramref name="frame"/> (from 1) with the effect's turn per frame.</summary>
    public void SetClock(int frame, float spinPerFrame)
    {
        _material.SetShaderParameter(_time, (frame - 1) / AnimationRetiming.FramesPerSecond);
        _material.SetShaderParameter(_spin, frame * spinPerFrame);
    }
}
