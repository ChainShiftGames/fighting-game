using Sim.Math;

namespace Sim.Data.Characters.Compiled;

/// <summary>One box of a move, present from <see cref="FirstFrame"/> to <see cref="LastFrame"/> (1-based).</summary>
public readonly record struct BoxData
{
    public BoxKind Kind { get; init; }

    public int FirstFrame { get; init; }

    public int LastFrame { get; init; }

    /// <summary>Relative to the fighter's origin while facing right; x forward, y up, in subpixels.</summary>
    public AxisAlignedBox Area { get; init; }
}
