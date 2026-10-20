using FightingGame.Presentation;
using Xunit;

namespace FightingGame.Tests.Presentation;

public sealed class VerticalFramingTests
{
    // The round-start view at 16:9: 228 world pixels, the floor 12% up from the bottom.
    private static readonly VerticalSpan _ground = new(-27_360, 200_640);

    // Testman (data): 120 px standing; airborne boxes from 10 to 110 px above his feet.
    private static readonly VerticalSpan _standing = new(0, 120_000);

    private static VerticalSpan Airborne(int feet) => new(feet, feet + 110_000);

    [Fact]
    public void GroundedFightersKeepTheGroundView()
    {
        Assert.Equal(_ground, VerticalFraming.Target(_ground, [_standing, _standing]));
        Assert.Equal(_ground, VerticalFraming.Target(_ground, [new VerticalSpan(0, 70_000), _standing]));
    }

    [Fact]
    public void ALowHopStaysInTheDeadZone() =>
        Assert.Equal(_ground, VerticalFraming.Target(_ground, [Airborne(50_000), _standing]));

    [Fact]
    public void AJumperOverAStandingFighterZoomsOutAndKeepsTheFloorAndBothFighters()
    {
        VerticalSpan apex = Airborne(210_000);

        VerticalSpan target = VerticalFraming.Target(_ground, [apex, _standing]);

        Assert.True(target.Top > apex.Top, "the jumper's head is in view");
        Assert.InRange(target.Bottom, _ground.Bottom, -1);
        Assert.True(target.Height > _ground.Height, "zoomed out");
    }

    [Fact]
    public void BothFightersHighRaiseTheViewWithoutZooming()
    {
        VerticalSpan apex = Airborne(210_000);

        VerticalSpan target = VerticalFraming.Target(_ground, [apex, apex]);

        Assert.Equal(_ground.Height, target.Height);
        Assert.True(target.Top > apex.Top, "both heads are in view");
        Assert.True(target.Bottom < apex.Bottom, "both fighters' feet are in view");
        Assert.True(target.Bottom > 0, "the floor has left the view");
    }

    [Fact]
    public void AnAirborneOpponentLetsTheViewRiseBeforeItZoomsOut()
    {
        VerticalSpan higher = Airborne(250_000);

        VerticalSpan overStanding = VerticalFraming.Target(_ground, [higher, _standing]);
        VerticalSpan overAirborne = VerticalFraming.Target(_ground, [higher, Airborne(150_000)]);

        Assert.Equal(overStanding.Top, overAirborne.Top);
        Assert.True(overAirborne.Height < overStanding.Height);
    }

    [Fact]
    public void ZoomedOutAllTheWayTheHighHeadWinsOverTheLowFeet()
    {
        VerticalSpan high = Airborne(500_000);
        VerticalSpan higher = Airborne(600_000);

        VerticalSpan target = VerticalFraming.Target(_ground, [high, _standing]);
        VerticalSpan furthest = VerticalFraming.Target(_ground, [higher, _standing]);

        Assert.Equal(target.Height, furthest.Height);
        Assert.True(furthest.Top > higher.Top, "the high head is in view");
        Assert.True(furthest.Bottom > 0, "the standing fighter's feet are not");
    }

    [Fact]
    public void RisesNoFurtherThanItsLimit()
    {
        VerticalSpan high = VerticalFraming.Target(_ground, [Airborne(1_000_000), Airborne(1_000_000)]);
        VerticalSpan higher = VerticalFraming.Target(_ground, [Airborne(1_500_000), Airborne(1_500_000)]);

        Assert.Equal(high.Bottom, higher.Bottom);
        Assert.Equal(high.Top, higher.Top);
    }

    [Fact]
    public void TheFightersOrderChangesNothing()
    {
        VerticalSpan jumper = Airborne(180_000);
        VerticalSpan falling = Airborne(60_000);

        Assert.Equal(
            VerticalFraming.Target(_ground, [jumper, falling]), VerticalFraming.Target(_ground, [falling, jumper]));
    }

    [Fact]
    public void MustShowBothFightersWholeWithinTheTarget()
    {
        VerticalSpan apex = Airborne(210_000);
        VerticalSpan target = VerticalFraming.Target(_ground, [apex, _standing]);

        VerticalSpan must = VerticalFraming.MustShow(target, [apex, _standing]);

        Assert.True(must.Top > apex.Top && must.Bottom < 0, "both fighters whole");
        Assert.True(must.Top <= target.Top && must.Bottom >= target.Bottom, "inside the target");
    }

    [Fact]
    public void MustShowNoMoreThanTheTargetShows()
    {
        VerticalSpan high = Airborne(600_000);
        VerticalSpan target = VerticalFraming.Target(_ground, [high, _standing]);

        VerticalSpan must = VerticalFraming.MustShow(target, [high, _standing]);

        Assert.Equal(target.Bottom, must.Bottom);
    }
}
