using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// Where the ice orb ends. A hit flashes white then cyan, bursts ice spikes, throws a cracked frost shockwave and
/// falling shards, and leaves a snow puff and glints. A block cracks against a hexagonal frost shield and its shards
/// bounce back, a clash is a smaller hit, and a fizzle melts into mist and falling snow.
/// </summary>
public partial class IceOrbImpactArt : Node3D, IImpactArt
{
    // Developer settings (presentation), picked in the VFX Lab (ice_orb sheet): how big the impact is drawn, its turn
    // per frame, and the card size in metres, room for the shards' and shockwave's reach.
    private const float ImpactScale = 0.84f;
    private const float SpinPerFrame = 0.27f;
    private static readonly Vector2 _cardSize = new(2.8f, 2.8f);
    private static readonly StringName _age = "age";
    private static readonly StringName _kind = "kind";
    private static readonly StringName _lifetime = "lifetime";

    private readonly ShaderCard _card = new();

    public IceOrbImpactArt()
    {
        _card.Build("ice_orb_impact", _cardSize, Vector2.Zero);
        _card.SetParameter(_lifetime, (float)Events.ProjectileImpacts.LifetimeFrames);
        AddChild(_card);
    }

    public void ShowFrame(int age, ImpactKind kind, Vector3 centre, int facing)
    {
        Position = centre;
        Scale = new Vector3(facing * ImpactScale, ImpactScale, ImpactScale);
        _card.SetClock(age + 1, SpinPerFrame);
        _card.SetParameter(_age, (float)age);
        _card.SetParameter(_kind, (int)kind);
    }
}
