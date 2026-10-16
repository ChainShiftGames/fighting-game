namespace Sim.Math;

/// <summary>A 2D integer vector, usually a position or velocity in subpixels.</summary>
/// <remarks>
/// Operators are plain <see cref="int"/> arithmetic and wrap on overflow, like C# ints: they run many times per
/// frame, and stage coordinates stay millions of subpixels away from the limits. Use <see cref="IntMath"/> where a
/// value can grow without bound.
/// </remarks>
public readonly record struct IntVector2(int X, int Y)
{
    public static IntVector2 Zero => default;

    /// <summary>Squared length as a long.</summary>
    /// <remarks>
    /// Saturates to <see cref="long.MaxValue"/> only for (int.MinValue, int.MinValue), whose squared length is
    /// exactly 2^63; <see cref="Length"/> is still exact there.
    /// </remarks>
    public long LengthSquared
    {
        get
        {
            long squaredX = (long)X * X;
            long squaredY = (long)Y * Y;
            return squaredX > long.MaxValue - squaredY ? long.MaxValue : squaredX + squaredY;
        }
    }

    /// <summary>Length rounded down.</summary>
    public long Length => IntMath.Sqrt(LengthSquared);

    /// <summary>Mirrored across the vertical axis, e.g. a forward offset for a fighter facing left.</summary>
    public IntVector2 MirrorX() => new(-X, Y);

    public static IntVector2 operator +(IntVector2 left, IntVector2 right) => new(left.X + right.X, left.Y + right.Y);

    public static IntVector2 operator -(IntVector2 left, IntVector2 right) => new(left.X - right.X, left.Y - right.Y);

    public static IntVector2 operator -(IntVector2 value) => new(-value.X, -value.Y);

    public static IntVector2 operator *(IntVector2 value, int scale) => new(value.X * scale, value.Y * scale);
}
