using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>One layer of an effect's particles (embers, flames, smoke…), each one's life worked out from the clock.</summary>
/// <remarks>
/// Godot's particle nodes run on elapsed time and keep their own history, so a rolled-back frame would not redraw
/// the same. Here the shader places every particle from <c>t</c> and its instance number alone; the node only sets
/// the clock, so a frame costs one uniform and never allocates.
/// </remarks>
public partial class FrameParticles : MultiMeshInstance3D
{
    private const string ShaderPath = "res://Assets/Effects/fire_orb_particles.gdshader";

    // Room the particles can fly in, in metres either side of the node, so Godot never culls a layer half on screen.
    private const float Reach = 4f;

    private static readonly StringName _time = "t";
    private static readonly StringName _kind = "kind";
    private static readonly StringName _count = "count";
    private static readonly StringName _amount = "amount";

    private readonly ShaderMaterial _material = new();

    // Godot needs a parameterless constructor for nodes; the layer comes in once, before the first frame.
    /// <param name="kind">The layer's behaviour: a <c>kind</c> listed in fire_orb_particles.gdshader.</param>
    /// <param name="count">How many particles the layer has at most.</param>
    /// <param name="priority">Draw order among the effect's see-through parts; higher draws over lower.</param>
    public void Build(int kind, int count, int priority)
    {
        _material.Shader = GD.Load<Shader>(ShaderPath);
        _material.RenderPriority = priority;
        _material.SetShaderParameter(_kind, kind);
        _material.SetShaderParameter(_count, count);
        Multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new QuadMesh { Size = Vector2.One },
            InstanceCount = count,
        };
        for (int i = 0; i < count; i++)
        {
            Multimesh.SetInstanceTransform(i, Transform3D.Identity);
        }

        MaterialOverride = _material;
        CastShadow = ShadowCastingSetting.Off;
        CustomAabb = new Aabb(-Vector3.One * Reach, Vector3.One * Reach * 2f);
    }

    public void SetParameter(StringName name, Variant value) => _material.SetShaderParameter(name, value);

    /// <param name="seconds">Seconds from the part's first frame, from the logic frame number.</param>
    /// <param name="amount">The share of the layer's particles shown, 0 to 1.</param>
    public void ShowFrame(float seconds, float amount)
    {
        _material.SetShaderParameter(_time, seconds);
        _material.SetShaderParameter(_amount, amount);
    }
}
