using System;
using System.Collections.Generic;
using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>The projectile effects a character's art file can name (developer setting, presentation).</summary>
/// <remarks>Add an effect by writing its node and one line here.</remarks>
public static class ProjectileEffects
{
    private static readonly Dictionary<string, Func<Node3D>> _effects = new(StringComparer.Ordinal)
    {
        ["dark_fireball"] = () => new DarkFireballArt(),
    };

    public static bool Exists(string name) => _effects.ContainsKey(name);

    /// <summary>A new node for the effect called <paramref name="name"/>; null when there is none.</summary>
    /// <param name="art">The same node, to show frames on.</param>
    public static Node3D? Create(string name, out IProjectileArt? art)
    {
        Node3D? node = _effects.TryGetValue(name, out Func<Node3D>? create) ? create() : null;
        art = node as IProjectileArt;
        return node;
    }
}
