using Sim.Math;
using Xunit;

namespace Sim.Tests.Math;

public sealed class SubpixelsTests
{
    [Theory]
    [InlineData(3, 3000)]
    [InlineData(-3, -3000)]
    [InlineData(2147483, 2147483000)]
    [InlineData(2147484, int.MaxValue)]
    [InlineData(-2147484, int.MinValue)]
    public void FromPixelsScalesAndSaturates(int pixels, int expected)
    {
        Assert.Equal(expected, Subpixels.FromPixels(pixels));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(999, 0)]
    [InlineData(1000, 1)]
    [InlineData(-1, -1)]
    [InlineData(-1000, -1)]
    [InlineData(-1001, -2)]
    [InlineData(int.MinValue, -2147484)]
    [InlineData(int.MaxValue, 2147483)]
    public void ToPixelsRoundsTowardNegativeInfinity(int subpixels, int expected)
    {
        Assert.Equal(expected, Subpixels.ToPixels(subpixels));
    }
}
