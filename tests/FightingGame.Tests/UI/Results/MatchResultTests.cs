using FightingGame.UI.Results;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.UI.Results;

public sealed class MatchResultTests
{
    [Fact]
    public void ReadsTheWinnerAndRoundsOfAFinishedMatch()
    {
        RoundState round = Over(Winners.Only(1), player1Rounds: 1, player2Rounds: 2);

        Assert.Equal(new MatchResult(MatchOutcome.Player2Wins, 1, 2), MatchResult.From(in round));
        Assert.Equal(1, MatchResult.From(in round).WinnerSide);
    }

    [Fact]
    public void BothWinnersAreADraw()
    {
        RoundState round = Over(Winners.Both, player1Rounds: 2, player2Rounds: 2);

        Assert.Equal(MatchOutcome.Draw, MatchResult.From(in round).Outcome);
        Assert.Equal(-1, MatchResult.From(in round).WinnerSide);
    }

    [Fact]
    public void AMatchStillBeingFoughtHasNoResult()
    {
        RoundState round = Over(Winners.Only(0), player1Rounds: 1, player2Rounds: 0);
        round.Phase = RoundPhase.Fighting;

        Assert.False(MatchResult.From(in round).IsFinished);
    }

    private static RoundState Over(Winners winners, int player1Rounds, int player2Rounds)
    {
        var round = new RoundState { Phase = RoundPhase.MatchOver, MatchWinners = winners };
        round.Wins[0] = player1Rounds;
        round.Wins[1] = player2Rounds;
        return round;
    }
}
