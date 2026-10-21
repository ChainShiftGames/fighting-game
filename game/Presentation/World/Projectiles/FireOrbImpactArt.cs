using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// Where the fire orb ends. A hit flashes white and erupts into a boiling fireball with a shockwave, a ring of fire
/// along the floor and impact rays, throwing embers and rolling flames that leave smoke hanging. A block splashes
/// against a barrier with fewer embers, a clash is a smaller hit, and a fizzle gutters out into smoke.
/// </summary>
public partial class FireOrbImpactArt : Node3D, IImpactArt
{
    // Developer settings (presentation): how big the impact is drawn, the card size in metres (room for the
    // shockwave's reach), the particles per layer at most, and the share of them a block, clash and fizzle throw.
    private const float ImpactScale = 1.15f;
    private const int Embers = 120;
    private const int Flames = 40;
    private const int Smoke = 24;
    private static readonly Vector2 _cardSize = new(3.4f, 3.4f);
    private static readonly float[] _emberShare = [1f, 0.45f, 0.6f, 0.1f];
    private static readonly float[] _flameShare = [1f, 0.3f, 0.6f, 0f];
    private static readonly float[] _smokeShare = [1f, 0.4f, 0.6f, 0.8f];

    private static readonly StringName _age = "age";
    private static readonly StringName _kind = "kind";
    private static readonly StringName _lifetime = "lifetime";

    private readonly ShaderCard _card = new();
    private readonly FrameParticles _embers = new();
    private readonly FrameParticles _flames = new();
    private readonly FrameParticles _smoke = new();

    public FireOrbImpactArt()
    {
        _card.Build("fire_orb_impact", _cardSize, Vector2.Zero);
        _card.SetParameter(_lifetime, (float)Events.ProjectileImpacts.LifetimeFrames);
        AddChild(_card);
        _smoke.Build(kind: 6, Smoke, priority: 1);
        _flames.Build(kind: 5, Flames, priority: 2);
        _embers.Build(kind: 4, Embers, priority: 3);
        AddChild(_smoke);
        AddChild(_flames);
        AddChild(_embers);
    }

    public void ShowFrame(int age, ImpactKind kind, Vector3 centre, int facing)
    {
        Position = centre;
        Scale = new Vector3(facing * ImpactScale, ImpactScale, ImpactScale);
        _card.SetClock(age + 1, 0f);
        _card.SetParameter(_age, (float)age);
        _card.SetParameter(_kind, (int)kind);
        float seconds = age / AnimationRetiming.FramesPerSecond;
        _embers.ShowFrame(seconds, _emberShare[(int)kind]);
        _flames.ShowFrame(seconds, _flameShare[(int)kind]);
        _smoke.ShowFrame(seconds, _smokeShare[(int)kind]);
    }
}
