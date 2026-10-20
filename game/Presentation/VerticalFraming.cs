using System;

namespace FightingGame.Presentation;

/// <summary>
/// How much of the world's height the camera aims to show for where the fighters are: the floor framing while they
/// stay low, rising with a high fighter, and zooming out only where rising alone would lose the other one.
/// </summary>
/// <remarks>
/// Presentation only: the logic camera has no height (ADR-0004 §5). When not everything fits, first wins: the limits
/// (rise, zoom-out); the highest head with headroom; the lowest feet with a strip of floor; the least zoom-out; the
/// least rise. So the floor stays put in grounded play, the view rises while both fighters fit in it (both airborne),
/// and it zooms out while one stands and the other is high, keeping both whole. It never zooms in past the ground view,
/// which already shows the whole logic screen. The fighters' order changes nothing.
/// </remarks>
public static class VerticalFraming
{
    private const int SubpixelsPerPixel = 1000;

    // Developer settings (presentation), in world pixels. The dead zone: the camera holds the ground view while every
    // head stays FollowHeadroomPixels or more below the view's top, then keeps that room above the highest head. Under
    // the lowest feet it keeps FloorStripPixels (a strip of floor while that fighter stands). EdgeMarginPixels is the
    // room the eased camera always leaves, however far it lags (VerticalFollow). The limits: how far the view's bottom
    // may rise above the ground view's, and how far it may zoom out (200 = twice the ground view's height). Testman's
    // jump over a standing fighter needs ~1.55 at its peak, his high jump ~2.05; 100 turns zooming off, and the
    // standing fighter's feet then leave the screen first.
    private const int FollowHeadroomPixels = 24;
    private const int FloorStripPixels = 8;
    private const int EdgeMarginPixels = 8;
    private const int MaxRisePixels = 300;
    private const int MaxZoomOutPercent = 210;

    /// <summary>The view the camera eases toward, given the round-start <paramref name="ground"/> view.</summary>
    /// <param name="bodies">Each fighter's height (<see cref="FighterBoxes.Height"/>).</param>
    public static VerticalSpan Target(VerticalSpan ground, ReadOnlySpan<VerticalSpan> bodies)
    {
        int top = Math.Max(ground.Top, HighestTop(bodies) + Pixels(FollowHeadroomPixels));
        int highestBottom = ground.Bottom + Pixels(MaxRisePixels);
        int rising = Math.Min(top - ground.Height, LowestBottom(bodies) - Pixels(FloorStripPixels));
        int bottom = Math.Max(ground.Bottom, Math.Min(rising, highestBottom));
        int tallest = ground.Height * MaxZoomOutPercent / 100;
        if (top - bottom > tallest)
        {
            // Zoomed out all the way: the high head wins over the low feet, up to the rise limit.
            bottom = Math.Min(top - tallest, highestBottom);
            top = bottom + tallest;
        }

        return new VerticalSpan(bottom, top);
    }

    /// <summary>
    /// What the view must cover however far its easing lags behind <paramref name="target"/>: every fighter whole with
    /// a small margin, as far as the target shows them.
    /// </summary>
    public static VerticalSpan MustShow(VerticalSpan target, ReadOnlySpan<VerticalSpan> bodies)
    {
        int top = Math.Min(target.Top, HighestTop(bodies) + Pixels(EdgeMarginPixels));
        int bottom = Math.Max(target.Bottom, LowestBottom(bodies) - Pixels(EdgeMarginPixels));
        return new VerticalSpan(Math.Min(bottom, top), top);
    }

    private static int Pixels(int pixels) => pixels * SubpixelsPerPixel;

    private static int HighestTop(ReadOnlySpan<VerticalSpan> bodies)
    {
        int top = int.MinValue;
        foreach (VerticalSpan body in bodies)
        {
            top = Math.Max(top, body.Top);
        }

        return top;
    }

    private static int LowestBottom(ReadOnlySpan<VerticalSpan> bodies)
    {
        int bottom = int.MaxValue;
        foreach (VerticalSpan body in bodies)
        {
            bottom = Math.Min(bottom, body.Bottom);
        }

        return bottom;
    }
}
