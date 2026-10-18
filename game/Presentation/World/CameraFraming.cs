using System;
using Godot;

namespace FightingGame.Presentation.World;

/// <summary>What the 3D camera shows: the gameplay plane point at the middle of the screen and the plane height in view.</summary>
/// <remarks>
/// Taken from the 2D <see cref="StageCamera"/>, shake included, so the 3D scene and the 2D layers over it line up.
/// A camera facing the plane square-on maps it with one scale and offset, orthographic or perspective (ADR-0027 §4).
/// </remarks>
/// <param name="Centre">The point on the gameplay plane at the middle of the screen, in metres.</param>
/// <param name="VisibleHeight">How many metres of the plane show from the top of the screen to the bottom.</param>
public readonly record struct CameraFraming(Vector3 Centre, float VisibleHeight)
{
    public static CameraFraming Of(StageCamera camera, Vector2 viewportSize)
    {
        float centreX = ((viewportSize.X / 2f) - camera.FloorCentre.X) / camera.PixelsPerSubpixel;
        float centreY = (camera.FloorCentre.Y - (viewportSize.Y / 2f)) / camera.PixelsPerSubpixel;
        return new CameraFraming(
            new Vector3(WorldScale.ToMetres(centreX), WorldScale.ToMetres(centreY), 0f),
            WorldScale.ToMetres(viewportSize.Y / camera.PixelsPerSubpixel));
    }

    /// <summary>How far from the plane a perspective camera with this vertical field of view must be to frame it.</summary>
    public float PerspectiveDistance(float fieldOfViewDegrees) =>
        VisibleHeight / 2f / MathF.Tan(fieldOfViewDegrees * MathF.PI / 360f);
}
