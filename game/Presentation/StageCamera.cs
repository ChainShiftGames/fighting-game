using System;
using Godot;
using Sim.Collision;
using Sim.Math;
using LogicScreen = Sim.Camera.CameraFraming;

namespace FightingGame.Presentation;

/// <summary>Turns world positions (subpixels, y up, origin on the floor at stage centre) into screen pixels.</summary>
/// <remarks>
/// The horizontal focus is the logic camera (<c>MatchState.Camera</c>, ADR-0004 §5); this only fits it to the
/// viewport and stops at the walls. Zoom, shake and flashes stay here. The zoom always shows the whole logic screen
/// (<c>MatchRules.MaxPlayerDistance</c>), since its edges hold fighters like walls.
/// </remarks>
/// <param name="FloorCentre">Where the world origin (stage centre, on the floor) is on screen.</param>
public readonly record struct StageCamera(Vector2 FloorCentre, float PixelsPerSubpixel)
{
    private const float SubpixelsPerPixel = 1000f;

    // Developer settings (presentation): world pixels shown from the top of the screen to the bottom, where the
    // floor sits as a share of the screen height from the top, the least room shown past each logic screen edge, and
    // how much of the world beyond a wall may show. 228 px makes a 120 px standing fighter ~53% of the screen height,
    // as close as Guilty Gear Strive frames its fighters.
    private const float VisibleWorldHeight = 228f;
    private const float FloorFromTop = 0.88f;
    private const float ScreenEdgeMarginPixels = 12f;
    private const float BeyondWallPixels = 16f;

    /// <summary>The camera for a viewport of <paramref name="size"/> pixels, centred on <paramref name="focusX"/>.</summary>
    /// <remarks>
    /// Zooms out further only when the viewport is too narrow for the logic screen (e.g. 16:10). Stops where a wall
    /// reaches the screen edge; a stage narrower than the view stays centred.
    /// </remarks>
    public static StageCamera Fit(Vector2 size, LogicScreen screen, int focusX)
    {
        float byHeight = size.Y / (VisibleWorldHeight * SubpixelsPerPixel);
        float byWidth = size.X / (screen.Width + (2f * ScreenEdgeMarginPixels * SubpixelsPerPixel));
        float pixelsPerSubpixel = Math.Min(byHeight, byWidth);
        StageWalls walls = screen.Stage;
        float halfView = size.X / 2f / pixelsPerSubpixel;
        float beyondWall = BeyondWallPixels * SubpixelsPerPixel;
        float lowest = walls.Left - beyondWall + halfView;
        float highest = walls.Right + beyondWall - halfView;
        float centreX = lowest > highest ? (walls.Left + walls.Right) / 2f : Math.Clamp(focusX, lowest, highest);
        return new StageCamera(
            new Vector2((size.X / 2f) - (centreX * pixelsPerSubpixel), size.Y * FloorFromTop), pixelsPerSubpixel);
    }

    public Vector2 ToScreen(IntVector2 world) =>
        FloorCentre + (new Vector2(world.X, -world.Y) * PixelsPerSubpixel);

    /// <summary>The box on screen; y flips, so the box's top (MaxY) becomes the rectangle's top edge.</summary>
    public Rect2 ToScreen(AxisAlignedBox box) =>
        new(ToScreen(new IntVector2(box.MinX, box.MaxY)), new Vector2(box.Width, box.Height) * PixelsPerSubpixel);
}
