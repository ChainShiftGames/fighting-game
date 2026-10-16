using Sim.Math;
using Xunit;

namespace Sim.Tests.Math;

public sealed class AxisAlignedBoxTests
{
    private static readonly AxisAlignedBox _box = AxisAlignedBox.FromCorner(0, 0, 10, 20);

    [Fact]
    public void FromCornerSetsSize()
    {
        Assert.Equal(new AxisAlignedBox(0, 0, 10, 20), _box);
        Assert.Equal(10, _box.Width);
        Assert.Equal(20, _box.Height);
    }

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(9, 19, true)]
    [InlineData(10, 0, false)]
    [InlineData(-10, 0, false)]
    [InlineData(0, 20, false)]
    [InlineData(0, -20, false)]
    [InlineData(11, 0, false)]
    public void TouchingEdgesDoNotOverlap(int offsetX, int offsetY, bool expected)
    {
        AxisAlignedBox other = _box.Translate(new IntVector2(offsetX, offsetY));

        Assert.Equal(expected, _box.Overlaps(other));
        Assert.Equal(expected, other.Overlaps(_box));
    }

    [Fact]
    public void EmptyBoxesNeverOverlap()
    {
        AxisAlignedBox zeroWidth = new(5, 5, 5, 15);
        AxisAlignedBox inverted = new(8, 8, 2, 12);

        Assert.True(zeroWidth.IsEmpty);
        Assert.True(inverted.IsEmpty);
        Assert.False(_box.IsEmpty);
        Assert.False(_box.Overlaps(zeroWidth));
        Assert.False(zeroWidth.Overlaps(_box));
        Assert.False(_box.Overlaps(inverted));
        Assert.False(inverted.Overlaps(inverted));
    }

    [Fact]
    public void MirrorXFlipsAcrossTheVerticalAxis()
    {
        AxisAlignedBox forward = new(20, 0, 50, 10);

        Assert.Equal(new AxisAlignedBox(-50, 0, -20, 10), forward.MirrorX());
        Assert.Equal(forward, forward.MirrorX().MirrorX());
    }
}
