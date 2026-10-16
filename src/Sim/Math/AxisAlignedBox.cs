namespace Sim.Math;

/// <summary>An integer axis-aligned box in subpixels, from its minimum corner to its maximum corner.</summary>
/// <remarks>
/// Edges are exclusive for overlap: boxes that only touch don't overlap, so two fighters standing flush against
/// each other aren't colliding. A box with Max ≤ Min on either axis is empty and never overlaps anything.
/// </remarks>
public readonly record struct AxisAlignedBox(int MinX, int MinY, int MaxX, int MaxY)
{
    public int Width => MaxX - MinX;

    public int Height => MaxY - MinY;

    /// <summary>A box at <paramref name="x"/>, <paramref name="y"/> (its minimum corner) with the given size.</summary>
    public static AxisAlignedBox FromCorner(int x, int y, int width, int height) => new(x, y, x + width, y + height);

    /// <summary>True when Max ≤ Min on either axis: the box covers no area.</summary>
    public bool IsEmpty => MaxX <= MinX || MaxY <= MinY;

    public bool Overlaps(AxisAlignedBox other) =>
        !IsEmpty && !other.IsEmpty
        && MinX < other.MaxX && other.MinX < MaxX && MinY < other.MaxY && other.MinY < MaxY;

    public AxisAlignedBox Translate(IntVector2 offset) =>
        new(MinX + offset.X, MinY + offset.Y, MaxX + offset.X, MaxY + offset.Y);

    /// <summary>Mirrored across the vertical axis (x = 0): a box defined facing right, turned to face left.</summary>
    public AxisAlignedBox MirrorX() => new(-MaxX, MinY, -MinX, MaxY);
}
