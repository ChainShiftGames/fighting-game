using System;
using System.Collections.Generic;
using Godot;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>The projectile effects a character's art file can name, by part (developer setting, presentation).</summary>
/// <remarks>Add an effect by writing its node and one line in the part's list here.</remarks>
public static class ProjectileEffects
{
    private static readonly Dictionary<string, Func<Node3D>> _flights = new(StringComparer.Ordinal)
    {
        ["dark_fireball"] = () => new DarkFireballArt(),
        ["ice_orb"] = () => new IceOrbArt(),
    };

    private static readonly Dictionary<string, Func<Node3D>> _charges = new(StringComparer.Ordinal)
    {
        ["dark_fireball_charge"] = () => new DarkFireballChargeArt(),
        ["ice_orb_charge"] = () => new IceOrbChargeArt(),
    };

    private static readonly Dictionary<string, Func<Node3D>> _impacts = new(StringComparer.Ordinal)
    {
        ["dark_fireball_impact"] = () => new DarkFireballImpactArt(),
        ["ice_orb_impact"] = () => new IceOrbImpactArt(),
    };

    public static bool FlightExists(string name) => _flights.ContainsKey(name);

    public static bool ChargeExists(string name) => _charges.ContainsKey(name);

    public static bool ImpactExists(string name) => _impacts.ContainsKey(name);

    /// <summary>A new node for the flight effect called <paramref name="name"/>; null when there is none.</summary>
    /// <param name="art">The same node, to show frames on.</param>
    public static Node3D? CreateFlight(string name, out IProjectileArt? art) => Create(_flights, name, out art);

    public static Node3D? CreateCharge(string name, out IChargeArt? art) => Create(_charges, name, out art);

    public static Node3D? CreateImpact(string name, out IImpactArt? art) => Create(_impacts, name, out art);

    private static Node3D? Create<TArt>(Dictionary<string, Func<Node3D>> effects, string name, out TArt? art)
        where TArt : class
    {
        Node3D? node = effects.TryGetValue(name, out Func<Node3D>? create) ? create() : null;
        art = node as TArt;
        return node;
    }
}
