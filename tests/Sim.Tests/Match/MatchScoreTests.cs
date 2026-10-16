using Sim.Match;
using Sim.Rules;
using Sim.State;
using Xunit;

namespace Sim.Tests.Match;

public sealed class MatchScoreTests
{
    private readonly MatchScore _score = new(new MatchRules { RoundsToWin = 2, MaxRounds = 5 });

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void TheRoundWinnerGetsARoundWin(int winner)
    {
        RoundState round = Score(1, 0, 0);

        _score.Award(ref round, Winners.Only(winner));

        Assert.Equal(1, round.Wins[winner]);
        Assert.Equal(0, round.Wins[1 - winner]);
    }

    [Fact]
    public void ADrawGivesBothPlayersARoundWin()
    {
        RoundState round = Score(1, 0, 0);

        _score.Award(ref round, Winners.Both);

        Assert.Equal((1, 1), (round.Wins[0], round.Wins[1]));
        Assert.False(_score.Result(in round).IsDecided);
    }

    [Fact]
    public void ADrawWinsTheMatchForTheOnlyPlayerOneWinAway()
    {
        RoundState round = Score(2, 1, 0);

        _score.Award(ref round, Winners.Both);

        Assert.Equal(Winners.Only(0), _score.Result(in round));
    }

    [Fact]
    public void ADrawThatWouldGiveBothTheirLastWinGivesNoneAndAnotherRoundFollows()
    {
        RoundState round = Score(3, 1, 1);

        _score.Award(ref round, Winners.Both);

        Assert.Equal((1, 1), (round.Wins[0], round.Wins[1]));
        Assert.False(_score.Result(in round).IsDecided);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ThePlayerWhoReachesTheWinCountWinsTheMatch(int winner)
    {
        RoundState round = Score(3, 1, 1);

        _score.Award(ref round, Winners.Only(winner));

        Assert.Equal(Winners.Only(winner), _score.Result(in round));
    }

    [Fact]
    public void AMatchStillUndecidedAfterTheLastRoundIsADraw()
    {
        RoundState round = Score(5, 1, 1);

        _score.Award(ref round, Winners.Both);

        Assert.True(_score.Result(in round).IsDraw);
    }

    private static RoundState Score(int round, int player1Wins, int player2Wins)
    {
        RoundState state = new() { Round = round };
        state.Wins[0] = player1Wins;
        state.Wins[1] = player2Wins;
        return state;
    }
}
