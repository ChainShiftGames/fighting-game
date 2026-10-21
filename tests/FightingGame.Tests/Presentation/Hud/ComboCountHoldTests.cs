using FightingGame.Presentation.Hud;
using Xunit;

namespace FightingGame.Tests.Presentation.Hud;

public sealed class ComboCountHoldTests
{
    [Fact]
    public void ShowsTheLiveCountThenHoldsTheFinishedOneForAWhile()
    {
        var hold = new ComboCountHold();

        Assert.Equal(3, hold.Follow(10, 3));
        Assert.Equal(3, hold.Follow(30, 0));
        Assert.Equal(0, hold.Follow(200, 0));
    }

    [Fact]
    public void ANewComboReplacesTheHeldCount()
    {
        var hold = new ComboCountHold();
        hold.Follow(10, 5);

        Assert.Equal(1, hold.Follow(20, 1));
    }

    [Fact]
    public void ARollbackToBeforeTheLastHitShowsWhatThatFrameHas()
    {
        var hold = new ComboCountHold();
        hold.Follow(10, 4);

        Assert.Equal(0, hold.Follow(8, 0));
    }
}
