using Sim.Math;
using Sim.Rules;

namespace Sim.Collision;

/// <summary>The left and right stage walls (x in subpixels); fighters' pushboxes stay between them.</summary>
/// <remarks>Walls reach infinitely high, so jumping fighters are stopped by them too. The corners are where they stand.</remarks>
public readonly record struct StageWalls(int Left, int Right)
{
    /// <summary>Walls at ±<see cref="MatchRules.WallDistance"/> from the stage centre.</summary>
    public static StageWalls From(in MatchRules rules) => new(-rules.WallDistance, rules.WallDistance);

    /// <summary>How far <paramref name="box"/> must move along x to be inside the walls: + right, - left, 0 inside.</summary>
    public int CorrectionFor(AxisAlignedBox box)
    {
        if (box.MinX < Left)
        {
            return Left - box.MinX;
        }

        return box.MaxX > Right ? Right - box.MaxX : 0;
    }
}
