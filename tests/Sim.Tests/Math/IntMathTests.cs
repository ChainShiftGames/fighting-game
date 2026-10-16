using System;
using Sim.Math;
using Xunit;

namespace Sim.Tests.Math;

public sealed class IntMathTests
{
    [Theory]
    [InlineData(5, 5)]
    [InlineData(-5, 5)]
    [InlineData(0, 0)]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(-int.MaxValue, int.MaxValue)]
    [InlineData(int.MinValue, int.MaxValue)]
    public void AbsSaturatesInsteadOfOverflowing(int value, int expected)
    {
        Assert.Equal(expected, IntMath.Abs(value));
    }

    [Theory]
    [InlineData(7, 1)]
    [InlineData(0, 0)]
    [InlineData(-7, -1)]
    [InlineData(int.MaxValue, 1)]
    [InlineData(int.MinValue, -1)]
    public void SignIsMinusOneZeroOrOne(int value, int expected)
    {
        Assert.Equal(expected, IntMath.Sign(value));
    }

    [Theory]
    [InlineData(5, 0, 10, 5)]
    [InlineData(-1, 0, 10, 0)]
    [InlineData(11, 0, 10, 10)]
    [InlineData(3, 3, 3, 3)]
    [InlineData(int.MinValue, int.MinValue, int.MaxValue, int.MinValue)]
    [InlineData(int.MaxValue, int.MinValue, int.MaxValue, int.MaxValue)]
    public void ClampLimitsToTheInclusiveRange(int value, int min, int max, int expected)
    {
        Assert.Equal(expected, IntMath.Clamp(value, min, max));
    }

    [Fact]
    public void ClampRejectsAnInvertedRange()
    {
        Assert.Throws<ArgumentException>(() => IntMath.Clamp(0, 1, -1));
    }

    [Theory]
    [InlineData(7, 2, 3)]
    [InlineData(-7, 2, -4)]
    [InlineData(7, -2, -4)]
    [InlineData(-7, -2, 3)]
    [InlineData(-6, 2, -3)]
    [InlineData(-1, 1000, -1)]
    [InlineData(0, -5, 0)]
    [InlineData(int.MinValue, 1, int.MinValue)]
    [InlineData(int.MinValue, 2, int.MinValue / 2)]
    [InlineData(int.MaxValue, -1, -int.MaxValue)]
    [InlineData(int.MinValue, -1, int.MaxValue)]
    public void DivFloorRoundsTowardNegativeInfinity(int dividend, int divisor, int expected)
    {
        Assert.Equal(expected, IntMath.DivFloor(dividend, divisor));
    }

    [Fact]
    public void DivFloorByZeroThrows()
    {
        Assert.Throws<DivideByZeroException>(() => IntMath.DivFloor(1, 0));
    }

    [Theory]
    [InlineData(1000, 80, 100, 800)]
    [InlineData(-5, 1, 2, -3)]
    [InlineData(5, -1, 2, -3)]
    [InlineData(-5, -1, 2, 2)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)]
    [InlineData(int.MinValue, int.MaxValue, int.MaxValue, int.MinValue)]
    [InlineData(int.MaxValue, 2, 1, int.MaxValue)]
    [InlineData(int.MinValue, 2, 1, int.MinValue)]
    [InlineData(int.MinValue, -1, 1, int.MaxValue)]
    public void MulDivAvoidsIntermediateOverflowRoundsDownAndSaturates(
        int value, int numerator, int denominator, int expected)
    {
        Assert.Equal(expected, IntMath.MulDiv(value, numerator, denominator));
    }

    [Theory]
    [InlineData(0L, 0)]
    [InlineData(1L, 1)]
    [InlineData(3L, 1)]
    [InlineData(4L, 2)]
    [InlineData(99L, 9)]
    [InlineData(100L, 10)]
    [InlineData((long)int.MaxValue, 46340)]
    [InlineData(4611686014132420609L, 2147483647)]
    [InlineData(4611686014132420608L, 2147483646)]
    [InlineData(long.MaxValue, 3037000499)]
    public void SqrtRoundsDown(long value, long expected)
    {
        Assert.Equal(expected, IntMath.Sqrt(value));
    }

    [Fact]
    public void SqrtOfANegativeNumberThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IntMath.Sqrt(-1));
    }

    [Theory]
    [InlineData(long.MaxValue, int.MaxValue)]
    [InlineData(long.MinValue, int.MinValue)]
    [InlineData(int.MaxValue + 1L, int.MaxValue)]
    [InlineData(int.MinValue - 1L, int.MinValue)]
    [InlineData(-42L, -42)]
    public void SaturateClampsToTheIntRange(long value, int expected)
    {
        Assert.Equal(expected, IntMath.Saturate(value));
    }
}
