using FightingGame.Presentation;
using Xunit;

namespace FightingGame.Tests.Presentation;

public sealed class VerticalFollowTests
{
    private static readonly VerticalSpan _ground = new(-27_360, 200_640);
    private static readonly VerticalSpan _standing = new(0, 120_000);
    private static readonly VerticalSpan[] _grounded = [_standing, _standing];

    // Over a standing opponent, near a jump's peak: the view is zoomed out.
    private static readonly VerticalSpan[] _jumping = [new(200_000, 310_000), _standing];

    private readonly VerticalFollow _follow = new();

    [Fact]
    public void TheFirstFrameCutsStraightToTheTarget() =>
        Assert.Equal(VerticalFraming.Target(_ground, _jumping), _follow.Follow(10, _ground, _jumping));

    [Fact]
    public void ShowingAFrameAgainChangesNothing()
    {
        _follow.Follow(0, _ground, _jumping);
        VerticalSpan eased = _follow.Follow(1, _ground, _grounded);

        Assert.Equal(eased, _follow.Follow(1, _ground, _grounded));
        Assert.Equal(eased, _follow.Follow(1, _ground, _grounded));
    }

    [Fact]
    public void SettlesPartOfTheWayEachFrameAndLandsExactlyOnTheTarget()
    {
        VerticalSpan previous = _follow.Follow(0, _ground, _jumping);
        int frame = 0;
        while (previous != _ground)
        {
            frame++;
            Assert.True(frame <= 60, "settled within a second");
            VerticalSpan view = _follow.Follow(frame, _ground, _grounded);

            Assert.NotEqual(previous, view);
            Assert.InRange(view.Top, _ground.Top, previous.Top);
            Assert.InRange(view.Height, _ground.Height, previous.Height);
            previous = view;
        }

        Assert.True(frame > 5, "eased, not cut");
        Assert.Equal(_ground, _follow.Follow(frame + 1, _ground, _grounded));
    }

    [Fact]
    public void FollowsLogicFramesNotDisplayedFrames()
    {
        var shownEveryFrame = new VerticalFollow();
        var shownTwiceAFrame = new VerticalFollow();
        shownEveryFrame.Follow(0, _ground, _jumping);
        shownTwiceAFrame.Follow(0, _ground, _jumping);

        for (int frame = 1; frame <= 4; frame++)
        {
            shownEveryFrame.Follow(frame, _ground, _grounded);
            shownTwiceAFrame.Follow(frame, _ground, _grounded);
            shownTwiceAFrame.Follow(frame, _ground, _grounded);
        }

        Assert.Equal(shownEveryFrame.Follow(5, _ground, _grounded), shownTwiceAFrame.Follow(5, _ground, _grounded));
    }

    [Fact]
    public void SkippedFramesEaseAsManySteps()
    {
        var stepped = new VerticalFollow();
        stepped.Follow(0, _ground, _jumping);
        _follow.Follow(0, _ground, _jumping);
        for (int frame = 1; frame < 4; frame++)
        {
            stepped.Follow(frame, _ground, _grounded);
        }

        Assert.Equal(stepped.Follow(4, _ground, _grounded), _follow.Follow(4, _ground, _grounded));
    }

    [Fact]
    public void AnEarlierFrameCutsStraightToTheTarget()
    {
        _follow.Follow(100, _ground, _jumping);
        _follow.Follow(101, _ground, _grounded);

        Assert.Equal(_ground, _follow.Follow(40, _ground, _grounded));
    }

    [Fact]
    public void RisesQuickerThanItSettles()
    {
        var rising = new VerticalFollow();
        rising.Follow(0, _ground, _grounded);
        _follow.Follow(0, _ground, _jumping);

        int rise = rising.Follow(1, _ground, _jumping).Top - _ground.Top;
        int settle = VerticalFraming.Target(_ground, _jumping).Top - _follow.Follow(1, _ground, _grounded).Top;

        Assert.True(rise > settle, $"rose {rise}, settled {settle}");
    }

    [Fact]
    public void KeepsAFastJumperAndItsStandingOpponentWholeWhileItLags()
    {
        _follow.Follow(0, _ground, _grounded);
        for (int frame = 1; frame <= 12; frame++)
        {
            // Rising 25 px a frame, faster than any jump: the easing alone would fall behind.
            var jumper = new VerticalSpan(frame * 25_000, (frame * 25_000) + 110_000);

            VerticalSpan view = _follow.Follow(frame, _ground, [jumper, _standing]);

            Assert.True(view.Top > jumper.Top, $"frame {frame}: the jumper's head is in view");
            Assert.True(view.Bottom < 0, $"frame {frame}: the opponent's feet are in view");
        }
    }
}
