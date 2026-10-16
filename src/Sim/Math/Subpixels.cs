namespace Sim.Math;

/// <summary>The logic's position unit: 1 pixel = 1000 subpixels (ADR-0003).</summary>
public static class Subpixels
{
    /// <summary>A fixed unit of the logic, not a tunable value.</summary>
    public const int PerPixel = 1000;

    /// <summary>Pixels to subpixels; saturates beyond ±2,147,483 pixels (<see cref="IntMath"/>).</summary>
    public static int FromPixels(int pixels) => IntMath.Saturate((long)pixels * PerPixel);

    /// <summary>Subpixels to whole pixels, rounded toward negative infinity: -1 subpixel is pixel -1.</summary>
    public static int ToPixels(int subpixels) => IntMath.DivFloor(subpixels, PerPixel);
}
