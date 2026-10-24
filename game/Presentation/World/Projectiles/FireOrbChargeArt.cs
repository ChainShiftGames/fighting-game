using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// The fire orb's startup: hot air sucks inward, a vortex of flame and a swarm of embers spiral in to the spawn point,
/// a white-hot seed swells and throbs faster, and the molten core forms, flaring, over the last part while a ring of
/// fire slams shut on it, so the orb takes over without a jump.
/// </summary>
public partial class FireOrbChargeArt : Node3D, IChargeArt
{
    // Developer settings (presentation): how big the whole charge is drawn, how fast the vortex turns, the share of the
    // startup after which the core starts to form (it doesn't roll until it flies), its glow while forming, the card
    // size in metres and how many embers spiral in.
    private const float ChargeScale = 1.2f;
    private const float SpinPerFrame = 0.3f;
    private const float CoreFormsFrom = 0.55f;
    private const float FormingFlare = 1.2f;
    private const int Motes = 60;

    private static readonly Vector2 _cardSize = new(2.8f, 2.8f);
    private static readonly StringName _progress = "progress";
    private static readonly StringName _spawnSize = "spawn_size";

    private readonly ShaderCard _card = new();
    private readonly ProjectileModel _core = new();
    private readonly FrameParticles _motes = new();

    public FireOrbChargeArt()
    {
        _card.Build("fire_orb_charge", _cardSize, Vector2.Zero);
        _card.SetParameter(_spawnSize, FireOrbArt.StartScale);
        AddChild(_card);
        _core.Build(FireOrbArt.Model);
        AddChild(_core);
        _motes.Build(kind: 7, Motes, priority: 3);
        AddChild(_motes);
    }

    public void ShowFrame(int moveFrame, int spawnFrame, Vector3 centre, int facing)
    {
        Position = centre;
        Scale = new Vector3(facing * ChargeScale, ChargeScale, ChargeScale);
        float progress = DarkFireballChargeArt.ProgressAt(moveFrame, spawnFrame);
        float seconds = (moveFrame - 1) / AnimationRetiming.FramesPerSecond;
        _card.SetClock(moveFrame, SpinPerFrame);
        _card.SetParameter(_progress, progress);
        _motes.ShowFrame(seconds, Mathf.Clamp(progress * 1.5f, 0f, 1f));
        float forming = Mathf.Clamp((progress - CoreFormsFrom) / (1f - CoreFormsFrom), 0f, 1f);
        _core.ShowFrame(FireOrbArt.Across * FireOrbArt.StartScale * forming, 0f, seconds, FormingFlare * (1f - forming));
    }
}
