using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// The floor fire a burning leg sweep leaves: the <c>fire_crescent</c> model laid on the floor where the foot swept,
/// lit along its arc as fast as the sweep and burning in the fire orb's colours, with flame tongues licking up off
/// it, soot rising and embers spat every way, many jumping out toward the camera. The sweep throws a spray of
/// sparks up as it lights; at the end of its life it dies down and breaks up. It lights an orange pool on the floor
/// and the fighters near it.
/// </summary>
/// <remarks>
/// The crescent's look settings are <c>fire_crescent</c>'s uniforms; the particles share its shape through
/// fire_orb_particles.gdshader's <c>arc_point</c>, so the two must keep the same size, tilt and sweep time.
/// </remarks>
public partial class FireCrescentArt : Node3D, IProjectileArt, IEffectLightSource
{
    // Developer settings (presentation): the crescent is Across metres wide and tipped Tilt radians up off the floor
    // toward the camera so it reads from the side; it lights end to end in SweepSeconds (match the leg sweep), with a
    // white-hot flash over FlareFrames, and dies down over its last DieFrames. Lift keeps it off the floor's surface.
    private const string Model = "res://Assets/Effects/fire_crescent/fire_crescent.glb";
    private const float Across = 1.8f;
    private const float Tilt = 0.75f;
    private const float SweepSeconds = 0.13f;
    private const int FlareFrames = 6;
    private const int DieFrames = 24;
    private const float Lift = 0.02f;

    // Developer settings (presentation): particles per layer, at most.
    private const int Flames = 110;
    private const int Embers = 160;
    private const int Smoke = 24;
    private const int Ignition = 140;

    // Developer settings (presentation): the floor pool's size in metres, the light on the fighters, its reach in
    // metres, its colour (above 1 is brighter) and how hard it flickers.
    private static readonly Vector2 _poolSize = new(4.6f, 4.6f);
    private const float PoolAboveFloor = 0.01f;
    private const float LightReach = 3.0f;
    private const float LightHeight = 0.4f;
    private const float Flicker = 0.25f;
    private static readonly Color _lightColor = new(2.0f, 0.7f, 0.18f);
    private static readonly Color _poolColor = new(1.0f, 0.42f, 0.08f);

    private static readonly StringName _burn = "burn";
    private static readonly StringName _sweepSeconds = "sweep_seconds";
    private static readonly StringName _arcSize = "arc_size";
    private static readonly StringName _arcTilt = "arc_tilt";
    private static readonly StringName _strength = "strength";
    private static readonly StringName _glowColor = "glow_color";
    private static readonly StringName _glints = "glints";
    private static readonly StringName _time = "t";

    // The crescent's bed: laid down to open back toward the fighter, then tipped up toward the camera.
    private readonly Node3D _bed = new();
    private readonly ProjectileModel _crescent = new();
    private readonly FrameParticles _flames = new();
    private readonly FrameParticles _embers = new();
    private readonly FrameParticles _smoke = new();
    private readonly FrameParticles _ignition = new();
    private readonly ShaderMaterial _poolMaterial = new()
    {
        Shader = GD.Load<Shader>("res://Assets/Effects/ice_orb_floor_glow.gdshader"),
    };

    private readonly MeshInstance3D _pool = new();
    private float _lightShare;

    public FireCrescentArt()
    {
        _bed.Basis = new Basis(Vector3.Right, Tilt - (Mathf.Pi / 2f)) * new Basis(Vector3.Up, Mathf.Pi);
        _bed.Position = new Vector3(0f, Lift, 0f);
        _crescent.Build(Model);
        _bed.AddChild(_crescent);
        AddChild(_bed);

        _smoke.Build(kind: 10, Smoke, priority: 1);
        _flames.Build(kind: 8, Flames, priority: 2);
        _embers.Build(kind: 9, Embers, priority: 3);
        _ignition.Build(kind: 11, Ignition, priority: 3);
        foreach (FrameParticles layer in new[] { _smoke, _flames, _embers, _ignition })
        {
            layer.SetParameter(_arcSize, Across);
            layer.SetParameter(_arcTilt, Tilt);
            layer.SetParameter(_sweepSeconds, SweepSeconds);
            AddChild(layer);
        }

        _pool.Mesh = new PlaneMesh { Size = _poolSize };
        _pool.MaterialOverride = _poolMaterial;
        _pool.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        _pool.Position = new Vector3(0f, PoolAboveFloor, 0f);
        _poolMaterial.SetShaderParameter(_glowColor, _poolColor);
        _poolMaterial.SetShaderParameter(_glints, 0f);
        AddChild(_pool);
    }

    public void ShowFrame(int frame, Vector3 centre, int facing, int lifetime)
    {
        // On the floor under its hitbox; mirrored, not turned, so it keeps facing the camera.
        Position = new Vector3(centre.X, 0f, centre.Z);
        Scale = new Vector3(facing, 1f, 1f);

        float seconds = (frame - 1) / AnimationRetiming.FramesPerSecond;
        float flare = 1f - Mathf.Clamp((frame - 1) / (float)FlareFrames, 0f, 1f);
        float burn = Mathf.Clamp((lifetime - frame) / (float)DieFrames, 0f, 1f);
        _crescent.ShowFrame(Across, 0f, seconds, flare);
        _crescent.SetParameter(_burn, burn);
        _flames.ShowFrame(seconds, burn);
        _embers.ShowFrame(seconds, Mathf.Sqrt(burn));
        _smoke.ShowFrame(seconds, Mathf.Min(1f, 0.4f + burn));
        _ignition.ShowFrame(seconds, 1f);

        float flicker = 1f - (Flicker * (0.5f + (0.3f * Mathf.Sin(frame * 1.3f)) + (0.2f * Mathf.Sin(frame * 3.7f))));
        float ignite = Mathf.Clamp(seconds / SweepSeconds, 0f, 1f);
        _lightShare = ignite * burn * flicker * (1f + flare);
        _poolMaterial.SetShaderParameter(_strength, _lightShare);
        _poolMaterial.SetShaderParameter(_time, seconds);
    }

    public bool LightNow(out Vector3 position, out Color color, out float radius)
    {
        position = Position + new Vector3(0f, LightHeight, 0f);
        color = _lightColor * (Visible ? _lightShare : 0f);
        radius = LightReach;
        return Visible && _lightShare > 0f;
    }
}
