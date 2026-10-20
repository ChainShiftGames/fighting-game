using FightingGame.Presentation;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Presentation;

public sealed class RoundBannerTextTests
{
    [Fact]
    public void NothingWhileFighting()
    {
        RoundState round = default;

        Assert.Equal(string.Empty, RoundBannerText.Headline(in round));
        Assert.Equal(string.Empty, RoundBannerText.Detail(in round));
    }

    [Fact]
    public void AKnockOutNamesTheRoundWinner()
    {
        RoundState round = Over(RoundEnding.KnockOut, Winners.Only(1));

        Assert.Equal("K.O.", RoundBannerText.Headline(in round));
        Assert.Equal("PLAYER 2 WINS THE ROUND", RoundBannerText.Detail(in round));
    }

    [Fact]
    public void ADoubleKnockOutIsADraw()
    {
        RoundState round = Over(RoundEnding.KnockOut, Winners.Both);

        Assert.Equal("DOUBLE K.O.", RoundBannerText.Headline(in round));
        Assert.Equal("DRAW", RoundBannerText.Detail(in round));
    }

    [Fact]
    public void ATimeOutNamesTheHealthLeader()
    {
        RoundState round = Over(RoundEnding.TimeOut, Winners.Only(0));

        Assert.Equal("TIME OUT", RoundBannerText.Headline(in round));
        Assert.Equal("PLAYER 1 WINS THE ROUND", RoundBannerText.Detail(in round));
    }

    [Fact]
    public void TheMatchOverBannerNamesTheMatchWinner()
    {
        RoundState round = Over(RoundEnding.KnockOut, Winners.Only(0));
        round.Phase = RoundPhase.MatchOver;
        round.MatchWinners = Winners.Only(0);

        Assert.Equal("PLAYER 1 WINS", RoundBannerText.Headline(in round));
        Assert.Equal(string.Empty, RoundBannerText.Detail(in round));

        round.MatchWinners = Winners.Both;
        Assert.Equal("DRAW GAME", RoundBannerText.Headline(in round));
    }

    private static RoundState Over(RoundEnding ending, Winners winners) => new()
    {
        Phase = RoundPhase.RoundOver,
        Ending = ending,
        RoundWinners = winners,
    };
}
