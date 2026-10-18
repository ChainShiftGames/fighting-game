using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// Where the dark fireball ends. A hit flashes white then negative, bursts into a jagged black sphere that
/// shatters into shards, throws impact lines and embers, and sends a shockwave rippling the screen. A block is
/// smaller and bursts back along a pale barrier arc, a clash is a smaller hit, and a fizzle collapses into itself.
/// </summary>
public partial class DarkFireballImpactArt : Node3D, IImpactArt
{
    // Developer setting (presentation): the card size in metres, room for the shards' and shockwave's reach.
    private static readonly Vector2 _cardSize = new(2.6f, 2.6f);
    private static readonly StringName _age = "age";
    private static readonly StringName _kind = "kind";
    private static readonly StringName _lifetime = "lifetime";

    private readonly ShaderCard _card = new();

    public DarkFireballImpactArt()
    {
        _card.Build("dark_fireball_impact", _cardSize, Vector2.Zero);
        _card.SetParameter(_lifetime, (float)Events.ProjectileImpacts.LifetimeFrames);
        AddChild(_card);
    }

    public void ShowFrame(int age, ImpactKind kind, Vector3 centre, int facing)
    {
        Position = centre;
        Scale = new Vector3(facing, 1f, 1f);
        _card.SetClock(age + 1, 0f);
        _card.SetParameter(_age, (float)age);
        _card.SetParameter(_kind, (int)kind);
    }
}
