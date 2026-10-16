using System;

namespace Sim.Math;

/// <summary>Integer arithmetic for the game logic with fixed, documented rounding and overflow behaviour.</summary>
/// <remarks>
/// Rounding: every division here rounds toward negative infinity (floor), unlike C#'s <c>/</c> which truncates
/// toward zero. Floor keeps results consistent across zero: a fighter at -1 subpixel is in pixel -1, not 0.
/// Overflow: a result that doesn't fit in an <see cref="int"/> saturates to <see cref="int.MinValue"/> or
/// <see cref="int.MaxValue"/> instead of wrapping or throwing, so an out-of-range value can't flip sign mid-match.
/// Division by zero throws <see cref="DivideByZeroException"/>, as for <c>/</c>; it is always a bug.
/// </remarks>
public static class IntMath
{
    /// <summary>Absolute value; <see cref="int.MinValue"/> saturates to <see cref="int.MaxValue"/>.</summary>
    public static int Abs(int value) => value switch
    {
        int.MinValue => int.MaxValue,
        < 0 => -value,
        _ => value,
    };

    /// <summary>-1, 0 or 1.</summary>
    public static int Sign(int value) => (value > 0 ? 1 : 0) - (value < 0 ? 1 : 0);

    /// <summary>Limits a value to [min, max]. Throws when <paramref name="min"/> is above <paramref name="max"/>.</summary>
    public static int Clamp(int value, int min, int max)
    {
        if (min > max)
        {
            throw new ArgumentException($"Clamp range is inverted: min {min} > max {max}", nameof(min));
        }

        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    /// <summary>Division rounded toward negative infinity; <c>int.MinValue / -1</c> saturates.</summary>
    public static int DivFloor(int dividend, int divisor)
    {
        if (dividend == int.MinValue && divisor == -1)
        {
            return int.MaxValue;
        }

        int quotient = dividend / divisor;
        bool hasRemainder = quotient * divisor != dividend;
        return hasRemainder && (dividend < 0) != (divisor < 0) ? quotient - 1 : quotient;
    }

    /// <summary>
    /// <c>value * numerator / denominator</c> without intermediate overflow (e.g. scaling by a percentage),
    /// rounded toward negative infinity and saturated to the <see cref="int"/> range.
    /// </summary>
    public static int MulDiv(int value, int numerator, int denominator)
    {
        // Two ints multiply to at most 2^62 in magnitude, so the long product can't overflow.
        long product = (long)value * numerator;
        long quotient = product / denominator;
        bool hasRemainder = quotient * denominator != product;
        if (hasRemainder && (product < 0) != (denominator < 0))
        {
            quotient--;
        }

        return Saturate(quotient);
    }

    /// <summary>Square root rounded down. Throws for negative values.</summary>
    public static long Sqrt(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Square root of a negative number");
        }

        // Digit-by-digit method, two bits per step: exact for every long, no floating point.
        ulong remainder = (ulong)value;
        ulong root = 0;
        ulong bit = 1UL << 62;
        while (bit > remainder)
        {
            bit >>= 2;
        }

        while (bit != 0)
        {
            if (remainder >= root + bit)
            {
                remainder -= root + bit;
                root = (root >> 1) + bit;
            }
            else
            {
                root >>= 1;
            }

            bit >>= 2;
        }

        return (long)root;
    }

    /// <summary>Clamps a long to the <see cref="int"/> range.</summary>
    public static int Saturate(long value) => value switch
    {
        > int.MaxValue => int.MaxValue,
        < int.MinValue => int.MinValue,
        _ => (int)value,
    };
}
