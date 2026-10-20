using Sim.Events;
using Xunit;

namespace Sim.Tests.Events;

public sealed class HitEventDetailTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(7, 0)]
    [InlineData(7, 3)]
    [InlineData(HitEventDetail.MoveLimit - 1, 31)]
    public void TheMoveAndHitComeBackOut(int move, int hit)
    {
        int detail = HitEventDetail.Of(move, hit);

        Assert.Equal((move, hit), (HitEventDetail.MoveOf(detail), HitEventDetail.HitOf(detail)));
    }

    [Fact]
    public void AFirstHitIsTheMoveAlone() => Assert.Equal(12, HitEventDetail.Of(12, 0));

    [Fact]
    public void NoHitCountsAsTheFirst() => Assert.Equal(HitEventDetail.Of(12, 0), HitEventDetail.Of(12, -1));
}
