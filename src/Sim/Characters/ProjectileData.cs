using Sim.Math;

namespace Sim.Characters;

/// <summary>A projectile a move spawns: when, where, how it flies, how long it lasts and what it does.</summary>
/// <remarks>
/// Facing right: x forward, y up, in subpixels (speeds per frame); the projectile takes its owner's facing at spawn.
/// Its boxes are a range in <see cref="CharacterData.Boxes"/>, all hit boxes, with frames counted from the frame it
/// spawns on (1) to <see cref="Lifetime"/>. Only 4-byte fields, so the struct hashes byte for byte.
/// </remarks>
public readonly record struct ProjectileData
{
    /// <summary>The frame of the spawning move (1-based) the projectile appears on.</summary>
    public int SpawnFrame { get; init; }

    /// <summary>Where it appears, relative to the owner's origin.</summary>
    public IntVector2 Offset { get; init; }

    public IntVector2 Velocity { get; init; }

    /// <summary>Frames it exists, counting the spawn frame; it disappears on the frame after the last one.</summary>
    public int Lifetime { get; init; }

    /// <summary>Points it loses one of per frame it overlaps an opposing projectile; gone at 0.</summary>
    public int Durability { get; init; }

    public int BoxStart { get; init; }

    public int BoxCount { get; init; }

    /// <summary>What it does to the fighter it touches (one hit, then it disappears).</summary>
    public HitData Hit { get; init; }
}
