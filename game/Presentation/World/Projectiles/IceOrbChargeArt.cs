using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// The ice orb's startup: cold mist and frost motes spiral in to the spawn point, the frost sigil traces itself, a
/// seed grows to the orb's first-frame size, and the ice ring forms round it, flaring, over the last part, so the
/// orb takes over without a jump.
/// </summary>
public partial class IceOrbChargeArt : Node3D, IChargeArt
{
    // Developer settings (presentation), picked in the VFX Lab (ice_orb sheet): how big the whole charge is drawn, how
    // fast the motes and sigil turn, the share of the startup after which the ring starts to form (it doesn't roll
    // until it flies), its glow while forming, and the card size in metres.
    private const float ChargeScale = 1.3f;
    private const float SpinPerFrame = 0.32f;
    private const float RingFormsFrom = 0.6f;
    private const float FormingFlare = 0.8f;

    private static readonly Vector2 _cardSize = new(2.6f, 2.6f);
    private static readonly StringName _progress = "progress";
    private static readonly StringName _spawnSize = "spawn_size";

    private readonly ShaderCard _card = new();
    private readonly ProjectileModel _ring = new();

    public IceOrbChargeArt()
    {
        _card.Build("ice_orb_charge", _cardSize, Vector2.Zero);
        _card.SetParameter(_spawnSize, IceOrbArt.StartScale);
        AddChild(_card);
        _ring.Build(IceOrbArt.Model);
        AddChild(_ring);
    }

    public void ShowFrame(int moveFrame, int spawnFrame, Vector3 centre, int facing)
    {
        Position = centre;
        Scale = new Vector3(facing * ChargeScale, ChargeScale, ChargeScale);
        float progress = DarkFireballChargeArt.ProgressAt(moveFrame, spawnFrame);
        _card.SetClock(moveFrame, SpinPerFrame);
        _card.SetParameter(_progress, progress);
        float forming = Mathf.Clamp((progress - RingFormsFrom) / (1f - RingFormsFrom), 0f, 1f);
        _ring.ShowFrame(
            IceOrbArt.Across * IceOrbArt.StartScale * forming,
            0f,
            (moveFrame - 1) / AnimationRetiming.FramesPerSecond,
            FormingFlare * (1f - forming));
    }
}
