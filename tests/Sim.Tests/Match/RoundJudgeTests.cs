using Sim.Match;
using Sim.State;
using Xunit;

namespace Sim.Tests.Match;

public sealed class RoundJudgeTests
{
    [Fact]
    public void NobodyAtZeroHealthIsNoKnockOut()
    {
        FighterStates fighters = WithHealth(1, 1);

        Assert.False(RoundJudge.KnockOutWinners(in fighters).IsDecided);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void TheFighterStillStandingWinsByKnockOut(int knockedOut, int standing)
    {
        FighterStates fighters = default;
        fighters[knockedOut].Health = 0;
        fighters[standing].Health = 1;

        Assert.Equal(Winners.Only(standing), RoundJudge.KnockOutWinners(in fighters));
    }

    [Fact]
    public void ADoubleKnockOutIsADraw()
    {
        FighterStates fighters = WithHealth(0, 0);

        Assert.True(RoundJudge.KnockOutWinners(in fighters).IsDraw);
    }

    [Fact]
    public void TimeOutGoesToTheLargerShareOfMaxHealthNotTheMostHealth()
    {
        // P1 has half its 10000; P2 has more health (9000) but only 45% of its 20000.
        RoundJudge judge = new(RoundSetup.Character(10_000), RoundSetup.Character(20_000));
        FighterStates fighters = WithHealth(5_000, 9_000);

        Assert.Equal(Winners.Only(0), judge.TimeOutWinners(in fighters));
    }

    [Fact]
    public void EqualSharesAtTimeOutAreADraw()
    {
        RoundJudge judge = new(RoundSetup.Character(10_000), RoundSetup.Character(20_000));
        FighterStates fighters = WithHealth(5_000, 10_000);

        Assert.True(judge.TimeOutWinners(in fighters).IsDraw);
    }

    [Theory]
    [InlineData(7_000, 3_000)]
    [InlineData(3_000, 7_000)]
    public void SwappingThePlayersSwapsTheTimeOutWinner(int player1Health, int player2Health)
    {
        RoundJudge judge = new(RoundSetup.Character(10_000), RoundSetup.Character(10_000));
        FighterStates fighters = WithHealth(player1Health, player2Health);
        FighterStates swapped = WithHealth(player2Health, player1Health);

        int winner = player1Health > player2Health ? 0 : 1;
        Assert.Equal(Winners.Only(winner), judge.TimeOutWinners(in fighters));
        Assert.Equal(Winners.Only(1 - winner), judge.TimeOutWinners(in swapped));
    }

    [Fact]
    public void LargeHealthValuesDoNotOverflow()
    {
        RoundJudge judge = new(RoundSetup.Character(int.MaxValue), RoundSetup.Character(int.MaxValue));
        FighterStates fighters = WithHealth(int.MaxValue, int.MaxValue - 1);

        Assert.Equal(Winners.Only(0), judge.TimeOutWinners(in fighters));
    }

    private static FighterStates WithHealth(int player1, int player2)
    {
        FighterStates fighters = default;
        fighters[0].Health = player1;
        fighters[1].Health = player2;
        return fighters;
    }
}
