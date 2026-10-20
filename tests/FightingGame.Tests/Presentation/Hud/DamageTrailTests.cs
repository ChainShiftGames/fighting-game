using FightingGame.Presentation.Hud;
using Xunit;

namespace FightingGame.Tests.Presentation.Hud;

public sealed class DamageTrailTests
{
    [Fact]
    public void HoldsTheDamageWhileTheFighterIsInHitstun()
    {
        var trail = new DamageTrail();
        trail.Follow(1, 1f, isHeld: false);

        trail.Follow(2, 0.6f, isHeld: true);
        trail.Follow(30, 0.6f, isHeld: true);

        Assert.Equal(1f, trail.Fraction);
    }

    [Fact]
    public void DrainsToTheHealthByLogicFramesOnceTheComboEnds()
    {
        var trail = new DamageTrail();
        trail.Follow(1, 1f, isHeld: false);
        trail.Follow(2, 0.6f, isHeld: true);

        trail.Follow(3, 0.6f, isHeld: false);
        float afterOneFrame = trail.Fraction;
        trail.Follow(300, 0.6f, isHeld: false);

        Assert.InRange(afterOneFrame, 0.6f, 0.999f);
        Assert.Equal(0.6f, trail.Fraction);
    }

    [Fact]
    public void FollowsHealthUpAtOnce()
    {
        var trail = new DamageTrail();
        trail.Follow(1, 0.3f, isHeld: false);

        trail.Follow(2, 1f, isHeld: false);

        Assert.Equal(1f, trail.Fraction);
    }

    [Fact]
    public void StartsOverWhenAnEarlierFrameIsShown()
    {
        var trail = new DamageTrail();
        trail.Follow(10, 1f, isHeld: false);
        trail.Follow(11, 0.5f, isHeld: true);

        trail.Follow(8, 0.8f, isHeld: false);

        Assert.Equal(0.8f, trail.Fraction);
    }
}
