using Sim.Math;
using Xunit;

namespace Sim.Tests.Math;

public sealed class TrigonometryTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(256, Trigonometry.One)]
    [InlineData(512, 0)]
    [InlineData(768, -Trigonometry.One)]
    [InlineData(1024, 0)]
    [InlineData(-256, -Trigonometry.One)]
    [InlineData(128, 46341)]
    [InlineData(1, 402)]
    public void SinMatchesKnownValues(int angle, int expected)
    {
        Assert.Equal(expected, Trigonometry.Sin(angle));
    }

    [Theory]
    [InlineData(0, Trigonometry.One)]
    [InlineData(256, 0)]
    [InlineData(512, -Trigonometry.One)]
    [InlineData(int.MaxValue, 65535)]
    [InlineData(int.MinValue, Trigonometry.One)]
    public void CosIsSinShiftedByAQuarterTurnEvenAtTheIntLimits(int angle, int expected)
    {
        Assert.Equal(expected, Trigonometry.Cos(angle));
    }

    [Fact]
    public void SinIsOddAndPeriodicForEveryAngleInATurn()
    {
        for (int angle = -Trigonometry.FullTurn; angle <= Trigonometry.FullTurn; angle++)
        {
            Assert.Equal(-Trigonometry.Sin(angle), Trigonometry.Sin(-angle));
            Assert.Equal(Trigonometry.Sin(angle), Trigonometry.Sin(angle + Trigonometry.FullTurn));
        }
    }

    [Fact]
    public void SinStaysWithinOne()
    {
        for (int angle = 0; angle < Trigonometry.FullTurn; angle++)
        {
            Assert.InRange(Trigonometry.Sin(angle), -Trigonometry.One, Trigonometry.One);
        }
    }
}
