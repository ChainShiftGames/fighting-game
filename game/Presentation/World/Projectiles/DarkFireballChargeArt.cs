using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// The dark fireball's startup: dark streams and sparks rush in to the spawn point, the stage bends harder and harder
/// towards it, and a seed orb grows to the fireball's first-frame size while a pale ring snaps shut, so the
/// fireball takes over without a jump.
/// </summary>
public partial class DarkFireballChargeArt : Node3D, IChargeArt
{
    // Developer settings (presentation): how fast the streams turn, and the card size in metres.
    private const float SpinPerFrame = 0.25f;

    private static readonly Vector2 _cardSize = new(2.6f, 2.6f);
    private static readonly StringName _progress = "progress";
    private static readonly StringName _spawnSize = "spawn_size";

    private readonly ShaderCard _card = new();

    public DarkFireballChargeArt()
    {
        _card.Build("dark_fireball_charge", _cardSize, Vector2.Zero);
        _card.SetParameter(_spawnSize, DarkFireballArt.StartScale);
        AddChild(_card);
    }

    public void ShowFrame(int moveFrame, int spawnFrame, Vector3 centre, int facing)
    {
        Position = centre;
        Scale = new Vector3(facing, 1f, 1f);
        _card.SetClock(moveFrame, SpinPerFrame);
        _card.SetParameter(_progress, ProgressAt(moveFrame, spawnFrame));
    }

    /// <summary>0 on the move's first frame, 1 on the frame before the projectile spawns.</summary>
    public static float ProgressAt(int moveFrame, int spawnFrame) =>
        spawnFrame <= 2 ? 1f : Mathf.Clamp((moveFrame - 1) / (float)(spawnFrame - 2), 0f, 1f);
}
