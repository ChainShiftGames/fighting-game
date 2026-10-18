using FightingGame.Presentation.World;
using Xunit;

namespace FightingGame.Tests.Presentation.World;

public sealed class PoseFadeTests
{
    [Fact]
    public void FadesABasicMoveInFromThePoseShownBefore()
    {
        var fade = new PoseFade();
        fade.Observe(clip: 1, seconds: 0.3, fadeIn: false, matchFrame: 10);
        fade.Observe(clip: 2, seconds: 0, fadeIn: true, matchFrame: 11);

        Assert.Equal(1, fade.FromClip);
        Assert.Equal(0.3, fade.FromSeconds, 5);
        Assert.Equal(0.2f, fade.Weight(11, frames: 4), 4);
        Assert.Equal(0.8f, fade.Weight(14, frames: 4), 4);
        Assert.Equal(1f, fade.Weight(15, frames: 4), 4);
    }

    [Fact]
    public void SnapsIntoAnAttack()
    {
        var fade = new PoseFade();
        fade.Observe(1, 0.3, fadeIn: false, matchFrame: 10);
        fade.Observe(3, 0, fadeIn: false, matchFrame: 11);

        Assert.Equal(-1, fade.FromClip);
        Assert.Equal(1f, fade.Weight(11, frames: 4));
    }

    [Fact]
    public void DropsTheFadeWhenTheMatchGoesBack()
    {
        var fade = new PoseFade();
        fade.Observe(1, 0.3, fadeIn: false, matchFrame: 10);
        fade.Observe(2, 0, fadeIn: true, matchFrame: 11);
        fade.Observe(2, 0, fadeIn: true, matchFrame: 9);

        Assert.Equal(1f, fade.Weight(9, frames: 4));
    }
}
