using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// The ice orb: a spiked ice ring spinning like a sawblade (the <c>ice_orb</c> model with its crystal material) in a
/// frost halo, with a slow six-fold sigil at its back and a misty trail of snowflakes, shards and speed streaks. It
/// starts small and grows to full size over its first frames, as it leaves the caster's hands. It lights a cyan pool
/// on the floor under it and the fighters near it.
/// </summary>
/// <remarks>The ring's look settings are <c>ice_orb_crystal</c>'s uniforms, the rest <c>ice_orb</c>'s.</remarks>
public partial class IceOrbArt : Node3D, IProjectileArt, IEffectLightSource
{
    // Developer settings (presentation), picked in the VFX Lab (ice_orb sheet): the ring is Across metres wide before
    // the whole flight is scaled by FlightScale, rolls RollPerFrame radians a frame (forward, so it reads as rolling the
    // way it flies) and sits ModelInFront metres before the card, so the trail is behind it. It grows from StartScale
    // over GrowFrames (ease-out); the sigil and glints turn SpinPerFrame. The card covers the trail, halo and sigil.
    public const string Model = "res://Assets/Effects/ice_orb/ice_orb.glb";
    public const float Across = 0.6f;
    public const float StartScale = 0.16f;
    private const float FlightScale = 1.15f;
    private const int GrowFrames = 25;
    private const float RollPerFrame = -0.14f;
    private const float SpinPerFrame = 0.015f;
    private const float ModelInFront = 0.12f;

    // Developer settings (presentation): the floor pool's size in metres and how high the orb can fly before it fades
    // out; the light on the fighters, its reach in metres and its colour (above 1 is brighter).
    private static readonly Vector2 _poolSize = new(2.6f, 3.4f);
    private const float PoolFadeFrom = 1.2f;
    private const float PoolFadeTo = 2.8f;
    private const float PoolAboveFloor = 0.01f;
    private const float LightReach = 2.4f;
    private static readonly Color _lightColor = new(0.15f, 0.75f, 1.8f);

    private static readonly Vector2 _cardSize = new(3.2f, 1.3f);
    private static readonly Vector2 _cardCentre = new(-0.6f, 0f);
    private static readonly StringName _size = "size";
    private static readonly StringName _strength = "strength";
    private static readonly StringName _time = "t";

    // The card and ring, scaled together; the floor pool stays the size it is.
    private readonly Node3D _body = new() { Scale = Vector3.One * FlightScale };
    private readonly ShaderCard _card = new();
    private readonly ProjectileModel _ring = new();
    private readonly ShaderMaterial _poolMaterial = new()
    {
        Shader = GD.Load<Shader>("res://Assets/Effects/ice_orb_floor_glow.gdshader"),
    };

    private readonly MeshInstance3D _pool = new();
    private float _lightShare;

    public IceOrbArt()
    {
        _card.Build("ice_orb", _cardSize, _cardCentre);
        _body.AddChild(_card);
        _ring.Build(Model);
        _ring.Position = new Vector3(0f, 0f, ModelInFront);
        _body.AddChild(_ring);
        AddChild(_body);
        _pool.Mesh = new PlaneMesh { Size = _poolSize };
        _pool.MaterialOverride = _poolMaterial;
        _pool.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        AddChild(_pool);
    }

    public void ShowFrame(int frame, Vector3 centre, int facing, int lifetime)
    {
        Position = centre;

        // Mirrored, not turned, for an orb flying left, so it keeps facing the camera.
        Scale = new Vector3(facing, 1f, 1f);
        float size = SizeAt(frame);
        _card.SetClock(frame, SpinPerFrame);
        _card.SetParameter(_size, size);
        float seconds = (frame - 1) / AnimationRetiming.FramesPerSecond;
        _ring.ShowFrame(Across * size, frame * RollPerFrame, seconds, 0f);

        // The pool lies on the floor (y = 0) under the orb, fading as it flies higher.
        _lightShare = size * (1f - Mathf.SmoothStep(PoolFadeFrom, PoolFadeTo, centre.Y));
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
