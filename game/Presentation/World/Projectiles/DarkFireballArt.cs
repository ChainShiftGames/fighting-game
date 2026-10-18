using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// The dark fireball ("Void Vortex"): a black void with swirling veins and rim lightning, an uneven heartbeat with
/// shockwaves, sparks pulled in and swallowed, one spiral aura arm, and the stage bending round it. It starts small
/// and grows to full size over its first frames, as it leaves the caster's hands.
/// </summary>
/// <remarks>One <c>dark_fireball</c> shader card; the look settings are that shader's uniforms.</remarks>
public partial class DarkFireballArt : Node3D, IProjectileArt
{
    // Developer settings (presentation): it grows from StartScale over GrowFrames (ease-out), and its aura turns
    // SpinPerFrame radians a frame. The card covers the trail, the aura and the sparks' reach, in metres.
    public const float StartScale = 0.2f;
    private const int GrowFrames = 12;
    private const float SpinPerFrame = 0.08f;

    private static readonly Vector2 _cardSize = new(1.8f, 1.7f);
    private static readonly Vector2 _cardCentre = new(-0.05f, 0f);
    private static readonly StringName _size = "size";

    private readonly ShaderCard _card = new();

    public DarkFireballArt()
    {
        _card.Build("dark_fireball", _cardSize, _cardCentre);
        AddChild(_card);
    }

    public void ShowFrame(int frame, Vector3 centre, int facing)
    {
        Position = centre;

        // Mirrored, not turned, for a fireball flying left, so the card keeps facing the camera.
        Scale = new Vector3(facing, 1f, 1f);
        _card.SetClock(frame, SpinPerFrame);
        _card.SetParameter(_size, SizeAt(frame));
    }

    /// <summary>Its size as a share of full size on <paramref name="frame"/> of its life: small at spawn, eased up to 1.</summary>
    public static float SizeAt(int frame)
    {
        float grown = Mathf.Clamp((frame - 1) / (float)GrowFrames, 0f, 1f);
        float eased = 1f - ((1f - grown) * (1f - grown));
        return StartScale + ((1f - StartScale) * eased);
    }
}
