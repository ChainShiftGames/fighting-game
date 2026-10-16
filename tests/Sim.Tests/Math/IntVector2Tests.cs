using Sim.Math;
using Xunit;

namespace Sim.Tests.Math;

public sealed class IntVector2Tests
{
    [Fact]
    public void ArithmeticIsComponentWise()
    {
        IntVector2 a = new(3, -4);
        IntVector2 b = new(10, 20);

        Assert.Equal(new IntVector2(13, 16), a + b);
        Assert.Equal(new IntVector2(-7, -24), a - b);
        Assert.Equal(new IntVector2(-3, 4), -a);
        Assert.Equal(new IntVector2(9, -12), a * 3);
        Assert.Equal(new IntVector2(-3, -4), a.MirrorX());
    }

    [Fact]
    public void OperatorsWrapLikePlainInts()
    {
        IntVector2 sum = new IntVector2(int.MaxValue, 0) + new IntVector2(1, 0);

        Assert.Equal(int.MinValue, sum.X);
    }

    [Fact]
    public void LengthRoundsDown()
    {
        Assert.Equal(5, new IntVector2(3, -4).Length);
        Assert.Equal(1, new IntVector2(1, 1).Length);
    }

    [Fact]
    public void LengthSquaredSaturatesOnlyAtTheExtremeAndLengthStaysExact()
    {
        IntVector2 extreme = new(int.MinValue, int.MinValue);
        IntVector2 largestExact = new(int.MaxValue, int.MinValue);

        Assert.Equal(long.MaxValue, extreme.LengthSquared);
        Assert.Equal(3037000499, extreme.Length);
        Assert.Equal(((long)int.MaxValue * int.MaxValue) + (1L << 62), largestExact.LengthSquared);
    }
}
