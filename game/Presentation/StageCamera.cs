using Godot;
using Sim.Math;

namespace FightingGame.Presentation;

/// <summary>Turns world positions (subpixels, y up, origin on the floor at stage centre) into screen pixels.</summary>
/// <remarks>
/// Presentation only. The camera's horizontal position will come from the logic (ADR-0004 §5, P1-11); until then it
/// stays on the stage centre. Zoom, shake and flashes stay here.
/// </remarks>
public readonly record struct StageCamera(Vector2 FloorCentre, float PixelsPerSubpixel)
{
    private const float SubpixelsPerPixel = 1000f;

    // Developer settings (presentation): world pixels shown from the top of the screen to the bottom, and where the
    // floor sits as a share of the screen height from the top.
    private const float VisibleWorldHeight = 324f;
    private const float FloorFromTop = 0.85f;

    /// <summary>The camera for a viewport of <paramref name="size"/> pixels.</summary>
    public static StageCamera Fit(Vector2 size) =>
        new(new Vector2(size.X / 2f, size.Y * FloorFromTop), size.Y / VisibleWorldHeight / SubpixelsPerPixel);

    public Vector2 ToScreen(IntVector2 world) =>
        FloorCentre + (new Vector2(world.X, -world.Y) * PixelsPerSubpixel);

    /// <summary>The box on screen; y flips, so the box's top (MaxY) becomes the rectangle's top edge.</summary>
    public Rect2 ToScreen(AxisAlignedBox box) =>
        new(ToScreen(new IntVector2(box.MinX, box.MaxY)), new Vector2(box.Width, box.Height) * PixelsPerSubpixel);
}
