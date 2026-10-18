using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// A black fireball: a boiling core in black flames, a violet aura turning round it and a dark trail behind. It
/// starts small and grows to full size over its first frames, as it leaves the caster's hands.
/// </summary>
/// <remarks>
/// Cards facing the camera around one sphere, drawn with the shaders in <c>Assets/Effects</c>. Everything moves with
/// the projectile's frame, never elapsed time, so it follows rollback and hitstop (ARCHITECTURE.md §9).
/// </remarks>
public partial class DarkFireballArt : Node3D, IProjectileArt
{
    // Developer settings (presentation), sizes in metres at full size. It grows from StartScale over GrowFrames
    // (ease-out), the aura turns SpinPerFrame radians a frame, and the trail is WispCount puffs WispSpacing apart.
    private const float CoreRadius = 0.19f;
    private const float StartScale = 0.2f;
    private const int GrowFrames = 12;
    private const float SpinPerFrame = 0.22f;
    private const int WispCount = 8;
    private const float WispSpacing = 0.1f;
    private const float WispSize = 0.36f;
    private const float WispSway = 0.06f;
    private const float LightEnergy = 1.4f;
    private const float LightRange = 1.8f;

    private static readonly Vector2 _flameSize = new(1.05f, 0.62f);
    private static readonly Vector3 _flameOffset = new(-0.14f, 0f, -0.25f);
    private static readonly Vector2 _auraSize = new(1.25f, 1.25f);
    private static readonly Vector3 _auraOffset = new(0f, 0f, -0.4f);
    private static readonly Color _wispColor = new(0.12f, 0.02f, 0.22f);
    private static readonly Color _lightColor = new(0.55f, 0.2f, 1f);

    private static readonly StringName _time = "t";
    private static readonly StringName _spin = "spin";

    private readonly Node3D _body = new();
    private readonly ShaderMaterial _core = Material("dark_fireball_core", 0);
    private readonly ShaderMaterial _flame = Material("dark_fireball_flame", 0);
    private readonly ShaderMaterial _aura = Material("dark_fireball_aura", -2);
    private readonly ShaderMaterial _wisp = Material("dark_fireball_wisp", -1);
    private readonly MultiMesh _wisps = new()
    {
        TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
        UseColors = true,
        UseCustomData = true,
        InstanceCount = WispCount,
        Mesh = new QuadMesh { Size = Vector2.One },
    };

    private readonly OmniLight3D _light = new() { LightColor = _lightColor, OmniRange = LightRange, ShadowEnabled = false };

    public override void _Ready()
    {
        AddChild(_body);
        AddChild(_light);
        _body.AddChild(Shape(new MultiMeshInstance3D { Multimesh = _wisps }, _wisp, Vector3.Zero));
        _body.AddChild(Shape(new MeshInstance3D { Mesh = new QuadMesh { Size = _auraSize } }, _aura, _auraOffset));
        _body.AddChild(Shape(new MeshInstance3D { Mesh = new QuadMesh { Size = _flameSize } }, _flame, _flameOffset));
        var sphere = new SphereMesh { Radius = CoreRadius, Height = 2f * CoreRadius, RadialSegments = 32, Rings = 16 };
        _body.AddChild(Shape(new MeshInstance3D { Mesh = sphere }, _core, Vector3.Zero));
        for (int wisp = 0; wisp < WispCount; wisp++)
        {
            _wisps.SetInstanceCustomData(wisp, new Color(wisp * 0.37f, 0f, 0f, 0f));
        }
    }

    public void ShowFrame(int frame, Vector3 centre, int facing)
    {
        Position = centre;
        float size = SizeAt(frame);

        // Mirrored, not turned, for a fireball flying left: its cards and their depth order stay as authored.
        _body.Scale = new Vector3(facing * size, size, size);
        _light.LightEnergy = LightEnergy * size;

        float seconds = (frame - 1) / AnimationRetiming.FramesPerSecond;
        float spin = frame * SpinPerFrame;
        _core.SetShaderParameter(_time, seconds);
        _flame.SetShaderParameter(_time, seconds);
        _aura.SetShaderParameter(_time, seconds);
        _aura.SetShaderParameter(_spin, spin);
        _wisp.SetShaderParameter(_time, seconds);
        PlaceWisps(frame, spin);
    }

    /// <summary>Its size as a share of full size on <paramref name="frame"/> of its life: small at spawn, eased up to 1.</summary>
    public static float SizeAt(int frame)
    {
        float grown = Mathf.Clamp((frame - 1) / (float)GrowFrames, 0f, 1f);
        float eased = 1f - ((1f - grown) * (1f - grown));
        return StartScale + ((1f - StartScale) * eased);
    }

    // The trail: puffs further back are smaller and fainter, sway with the aura and appear one by one after spawning.
    private void PlaceWisps(int frame, float spin)
    {
        for (int wisp = 0; wisp < WispCount; wisp++)
        {
            float along = (wisp + 1f) / WispCount;
            float sway = Mathf.Sin((spin * 0.6f) + (wisp * 1.1f)) * WispSway * along;
            float size = WispSize * (1f - (0.55f * along));
            var offset = new Vector3(-(wisp + 1) * WispSpacing, sway, -0.3f - (wisp * 0.001f));
            _wisps.SetInstanceTransform(wisp, new Transform3D(Basis.Identity.Scaled(Vector3.One * size), offset));

            float appeared = Mathf.Clamp((frame - (wisp * 2f)) / 4f, 0f, 1f);
            _wisps.SetInstanceColor(wisp, _wispColor with { A = 0.85f * (1f - along + (0.5f / WispCount)) * appeared });
        }
    }

    private static ShaderMaterial Material(string shader, int renderPriority) => new()
    {
        Shader = GD.Load<Shader>($"res://Assets/Effects/{shader}.gdshader"),
        RenderPriority = renderPriority,
    };

    private static GeometryInstance3D Shape(GeometryInstance3D shape, ShaderMaterial material, Vector3 offset)
    {
        shape.MaterialOverride = material;
        shape.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        shape.Position = offset;
        return shape;
    }
}
