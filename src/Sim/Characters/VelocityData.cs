namespace Sim.Characters;

/// <summary>A move's own velocity from <see cref="FirstFrame"/> to <see cref="LastFrame"/> (1-based).</summary>
/// <remarks>
/// Facing right: <see cref="X"/> forward, <see cref="Y"/> up, in subpixels per frame. On the frames it covers it
/// replaces the fighter's velocity and gravity doesn't apply (dashes, backdashes, air dashes, lunging attacks).
/// </remarks>
public readonly record struct VelocityData
{
    public int FirstFrame { get; init; }

    public int LastFrame { get; init; }

    public int X { get; init; }

    public int Y { get; init; }
}
