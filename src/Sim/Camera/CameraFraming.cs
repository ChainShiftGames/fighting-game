using Sim.Collision;
using Sim.Math;
using Sim.Rules;

namespace Sim.Camera;

/// <summary>What the logic camera can see: its width, and how far it may move before it would show past a wall.</summary>
/// <param name="HalfWidth">Half the screen width in subpixels; the screen reaches this far either side of the camera.</param>
/// <param name="Stage">The stage walls the screen stays between.</param>
public readonly record struct CameraFraming(int HalfWidth, StageWalls Stage)
{
    /// <summary>A screen as wide as <see cref="MatchRules.MaxPlayerDistance"/> on the stage of <paramref name="rules"/>.</summary>
    /// <remarks>An odd max distance loses its last subpixel, so the screen stays symmetric around the camera.</remarks>
    public static CameraFraming From(in MatchRules rules) => new(rules.MaxPlayerDistance / 2, StageWalls.From(in rules));

    /// <summary>The full screen width: the most the fighters' pushboxes may span together.</summary>
    public int Width => 2 * HalfWidth;

    /// <summary>The screen edges with the camera at <paramref name="cameraX"/>; they hold fighters like walls.</summary>
    public StageWalls ScreenAt(int cameraX) => new(cameraX - HalfWidth, cameraX + HalfWidth);

    /// <summary>The camera centred on [<paramref name="left"/>, <paramref name="right"/>], kept from showing past a wall.</summary>
    public int Centre(int left, int right)
    {
        // C#'s division truncates toward zero, so mirrored fighters give a mirrored camera (floor would favour one side).
        int centre = (left + right) / 2;
        return IntMath.Clamp(centre, Stage.Left + HalfWidth, Stage.Right - HalfWidth);
    }
}
