using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// The fire orb: a boiling molten core (the <c>fire_orb</c> model with its core material) inside a sun-like corona
/// of flame tongues and flare arcs, pulsing out hot shockwaves, trailing a plume of fire and smoke through hot,
/// rippling air. Embers stream off it, fire puffs boil off it, soot rises off its trail and sparks orbit it, all as
/// 3D particles that pass in front of and behind the core. It starts small and grows to full size as it leaves the
/// caster's hands, and lights a flickering orange pool on the floor under it and the fighters near it.
/// </summary>
/// <remarks>The core's look settings are <c>fire_orb_core</c>'s uniforms, the card's <c>fire_orb</c>'s.</remarks>
public partial class FireOrbArt : Node3D, IProjectileArt, IEffectLightSource
{
    // Developer settings (presentation): the core is Across metres wide before the whole flight is scaled by
    // FlightScale and turns RollPerFrame radians a frame (forward, so it reads as rolling the way it flies), sitting
    // ModelInFront metres before the card so the corona wraps it. It grows from StartScale over GrowFrames (ease-out),
    // and flares white-hot as it's born, fading over FlareFrames. The card covers the trail, corona and shockwaves.
    public const string Model = "res://Assets/Effects/fire_orb/fire_orb.glb";
    public const float Across = 0.6f;
    public const float StartScale = 0.2f;
    private const float FlightScale = 1.1f;
    private const int GrowFrames = 14;
    private const int FlareFrames = 8;
    private const float RollPerFrame = -0.09f;
    private const float SpinPerFrame = 0.05f;
    private const float ModelInFront = 0.15f;

    // Developer settings (presentation): particles per layer, at most.
    private const int Embers = 140;
    private const int Flames = 70;
    private const int Smoke = 36;
    private const int OrbitSparks = 24;

    // Developer settings (presentation): the floor pool's size in metres and how high the orb can fly before it fades
    // out; the light on the fighters, its reach in metres, its colour (above 1 is brighter) and how hard it flickers.
    private static readonly Vector2 _poolSize = new(3.0f, 3.6f);
    private const float PoolFadeFrom = 1.2f;
    private const float PoolFadeTo = 3.0f;
    private const float PoolAboveFloor = 0.01f;
    private const float LightReach = 2.8f;
    private const float Flicker = 0.2f;
    private static readonly Color _lightColor = new(2.0f, 0.75f, 0.2f);
    private static readonly Color _poolColor = new(1.0f, 0.45f, 0.1f);

    private static readonly Vector2 _cardSize = new(3.6f, 1.8f);
    private static readonly Vector2 _cardCentre = new(-0.8f, 0f);
    private static readonly StringName _strength = "strength";
    private static readonly StringName _glowColor = "glow_color";
    private static readonly StringName _glints = "glints";
    private static readonly StringName _time = "t";

    // Everything that grows with the orb; the floor pool stays the size it is.
    private readonly Node3D _body = new();
    private readonly ShaderCard _card = new();
    private readonly ProjectileModel _core = new();
    private readonly FrameParticles _embers = new();
    private readonly FrameParticles _flames = new();
    private readonly FrameParticles _smoke = new();
    private readonly FrameParticles _orbitSparks = new();
    private readonly ShaderMaterial _poolMaterial = new()
    {
        Shader = GD.Load<Shader>("res://Assets/Effects/ice_orb_floor_glow.gdshader"),
    };

    private readonly MeshInstance3D _pool = new();
    private float _lightShare;

    public FireOrbArt()
    {
        _card.Build("fire_orb", _cardSize, _cardCentre);
        _body.AddChild(_card);
        _core.Build(Model);
        _core.Position = new Vector3(0f, 0f, ModelInFront);
        _body.AddChild(_core);
        _smoke.Build(kind: 2, Smoke, priority: 1);
        _flames.Build(kind: 1, Flames, priority: 2);
        _embers.Build(kind: 0, Embers, priority: 3);
        _orbitSparks.Build(kind: 3, OrbitSparks, priority: 3);
        foreach (FrameParticles layer in new[] { _smoke, _flames, _embers, _orbitSparks })
        {
            layer.Position = new Vector3(0f, 0f, ModelInFront);
            _body.AddChild(layer);
        }

        AddChild(_body);
        _pool.Mesh = new PlaneMesh { Size = _poolSize };
        _pool.MaterialOverride = _poolMaterial;
        _pool.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        _poolMaterial.SetShaderParameter(_glowColor, _poolColor);
        _poolMaterial.SetShaderParameter(_glints, 0f);
        AddChild(_pool);
    }

    public void ShowFrame(int frame, Vector3 centre, int facing, int lifetime)
    {
        Position = centre;

        // Mirrored, not turned, for an orb flying left, so it keeps facing the camera.
        Scale = new Vector3(facing, 1f, 1f);
        float size = SizeAt(frame);
        _body.Scale = Vector3.One * (FlightScale * size);
        float seconds = (frame - 1) / AnimationRetiming.FramesPerSecond;
        _card.SetClock(frame, SpinPerFrame);
        float flare = 1f - Mathf.Clamp((frame - 1) / (float)FlareFrames, 0f, 1f);
        _core.ShowFrame(Across, frame * RollPerFrame, seconds, flare);
        _embers.ShowFrame(seconds, 1f);
        _flames.ShowFrame(seconds, 1f);
        _smoke.ShowFrame(seconds, 1f);
        _orbitSparks.ShowFrame(seconds, 1f);

        // The pool lies on the floor (y = 0) under the orb, fading as it flies higher; it and the light flicker.
        float flicker = 1f - (Flicker * (0.5f + (0.3f * Mathf.Sin(frame * 1.7f)) + (0.2f * Mathf.Sin(frame * 4.3f))));
        _lightShare = size * (1f - Mathf.SmoothStep(PoolFadeFrom, PoolFadeTo, centre.Y)) * flicker;
        _pool.Position = new Vector3(0f, PoolAboveFloor - centre.Y, 0f);
        _poolMaterial.SetShaderParameter(_strength, _lightShare);
        _poolMaterial.SetShaderParameter(_time, seconds);
    }

    public bool LightNow(out Vector3 position, out Color color, out float radius)
    {
        position = Position;
        color = _lightColor * (Visible ? _lightShare : 0f);
        radius = LightReach;
        return Visible && _lightShare > 0f;
    }

    /// <summary>Its size as a share of full size on <paramref name="frame"/> of its life: small at spawn, eased up to 1.</summary>
    public static float SizeAt(int frame)
    {
        float grown = Mathf.Clamp((frame - 1) / (float)GrowFrames, 0f, 1f);
        return StartScale + ((1f - StartScale) * (1f - ((1f - grown) * (1f - grown))));
    }
}
