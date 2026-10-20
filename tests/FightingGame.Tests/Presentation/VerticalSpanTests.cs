using FightingGame.Presentation;
using Xunit;

namespace FightingGame.Tests.Presentation;

public sealed class VerticalSpanTests
{
    private static readonly VerticalSpan _view = new(0, 200_000);

    [Fact]
    public void CoveringWhatItAlreadyCoversChangesNothing() =>
        Assert.Equal(_view, _view.Covering(new VerticalSpan(10_000, 190_000)));

    [Fact]
    public void MovesUpToCoverAHigherSpanAndKeepsItsHeight() =>
        Assert.Equal(new VerticalSpan(30_000, 230_000), _view.Covering(new VerticalSpan(50_000, 230_000)));

    [Fact]
    public void MovesDownToCoverALowerSpanAndKeepsItsHeight() =>
        Assert.Equal(new VerticalSpan(-20_000, 180_000), _view.Covering(new VerticalSpan(-20_000, 150_000)));

    [Fact]
    public void MovesOnlyAsFarAsItUncoversNothingThenGrows() =>
        Assert.Equal(new VerticalSpan(5_000, 260_000), _view.Covering(new VerticalSpan(5_000, 260_000)));

    [Fact]
    public void GrowsToCoverATallerSpan() =>
        Assert.Equal(new VerticalSpan(-10_000, 300_000), _view.Covering(new VerticalSpan(-10_000, 300_000)));
}
