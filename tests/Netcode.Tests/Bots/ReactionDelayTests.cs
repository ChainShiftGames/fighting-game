using Netcode.Bots;
using Xunit;

namespace Netcode.Tests.Bots;

public sealed class ReactionDelayTests
{
    [Fact]
    public void NoDelaySeesEachObservationAtOnce()
    {
        var delay = new ReactionDelay(0);

        Assert.Equal(Seen(1), delay.Push(Seen(1)));
        Assert.Equal(Seen(2), delay.Push(Seen(2)));
    }

    [Fact]
    public void ADelayOfThreeSeesWhatHappenedThreeFramesAgo()
    {
        var delay = new ReactionDelay(3);
        for (int frame = 1; frame <= 3; frame++)
        {
            delay.Push(Seen(frame));
        }

        for (int frame = 4; frame <= 20; frame++)
        {
            Assert.Equal(Seen(frame - 3), delay.Push(Seen(frame)));
        }
    }

    [Fact]
    public void BeforeEnoughFramesTheFirstOneStandsIn()
    {
        var delay = new ReactionDelay(5);

        Assert.Equal(Seen(1), delay.Push(Seen(1)));
        Assert.Equal(Seen(1), delay.Push(Seen(2)));
        Assert.Equal(Seen(1), delay.Push(Seen(3)));
    }

    // The frame number rides in the health, just to tell observations apart.
    private static BotObservation Seen(int frame) =>
        new(true, default, new FighterView { Health = frame }, BotObservation.NoProjectile);
}
