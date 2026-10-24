using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>A 3D model drawn as part of a projectile effect, sized, rolled and clocked for one logic frame.</summary>
/// <remarks>
/// The model is 1 m across (tools/Blender/prop_to_glb.py); its material reads the clock from per-instance uniforms
/// <c>t</c> and <c>flare</c>, so the instances of one model never share a frame.
/// </remarks>
public partial class ProjectileModel : Node3D
{
    private static readonly StringName _time = "t";
    private static readonly StringName _flare = "flare";

    private GeometryInstance3D? _mesh;

    // Godot needs a parameterless constructor for nodes; the model comes in once, before the first frame.
    public void Build(string path)
    {
        Node model = GD.Load<PackedScene>(path).Instantiate();
        AddChild(model);
        _mesh = SceneSearch.First<GeometryInstance3D>(model);
        if (_mesh is not null)
        {
            _mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        }
    }

    /// <param name="across">Its width in metres; 0 hides it.</param>
    /// <param name="roll">Its turn about the camera's axis, in radians.</param>
    /// <param name="seconds">The material's clock: seconds from the part's first frame.</param>
    /// <param name="flare">Extra glow, 0 for none.</param>
    public void ShowFrame(float across, float roll, float seconds, float flare)
    {
        Visible = across > 0f;
        Scale = Vector3.One * Mathf.Max(across, 1e-3f);
        Rotation = new Vector3(0f, 0f, roll);
        _mesh?.SetInstanceShaderParameter(_time, seconds);
        _mesh?.SetInstanceShaderParameter(_flare, flare);
    }

    /// <summary>Sets one more per-instance uniform of the model's material, for this instance only.</summary>
    public void SetParameter(StringName name, Variant value) => _mesh?.SetInstanceShaderParameter(name, value);
}
