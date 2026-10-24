using FightingGame.UI.Framework;
using FightingGame.UI.Results;
using Xunit;

namespace FightingGame.Tests.UI.Results;

public sealed class ResultsChoicesTests
{
    private readonly ResultsChoices _choices = new();

    public ResultsChoicesTests()
    {
        _choices.Begin([ResultsEntry.Rematch, ResultsEntry.CharacterSelect, ResultsEntry.MainMenu], choosers: 2);
    }

    [Fact]
    public void RematchNeedsBothPlayers()
    {
        Assert.Null(_choices.Press(0, MenuAction.Confirm));
        Assert.True(_choices.IsIn(0));
        Assert.False(_choices.IsRematchAgreed(_ => true));

        Assert.Null(_choices.Press(1, MenuAction.Confirm));

        Assert.True(_choices.IsRematchAgreed(_ => true));
    }

    [Fact]
    public void APlayerWithoutAControllerCountsAsIn()
    {
        Assert.False(_choices.IsRematchAgreed(player => player == 0));

        _choices.Press(0, MenuAction.Confirm);

        Assert.True(_choices.IsRematchAgreed(player => player == 0));
        Assert.False(_choices.IsIn(1));
    }

    [Fact]
    public void NobodyWithAControllerNeverAgrees()
    {
        Assert.False(_choices.IsRematchAgreed(_ => false));
    }

    [Fact]
    public void BackTakesTheTickAway()
    {
        _choices.Press(0, MenuAction.Confirm);

        _choices.Press(0, MenuAction.Back);

        Assert.False(_choices.IsIn(0));
    }

    [Fact]
    public void ATickedPlayersCursorStaysOnRematch()
    {
        _choices.Press(0, MenuAction.Confirm);

        _choices.Press(0, MenuAction.Down);

        Assert.Equal(0, _choices.CursorOf(0));
        Assert.True(_choices.IsIn(0));
    }

    [Fact]
    public void EachPlayerMovesTheirOwnCursorAndOnePickTakesBoth()
    {
        _choices.Press(0, MenuAction.Confirm);
        _choices.Press(1, MenuAction.Down);
        _choices.Press(1, MenuAction.Down);
        _choices.Press(1, MenuAction.Down);

        Assert.Equal(0, _choices.CursorOf(0));
        Assert.Equal(2, _choices.CursorOf(1));
        Assert.Equal(ResultsEntry.MainMenu, _choices.Press(1, MenuAction.Confirm));
    }

    [Fact]
    public void OneChooserGetsRematchAtOnce()
    {
        _choices.Begin([ResultsEntry.Rematch, ResultsEntry.MainMenu], choosers: 1);

        Assert.Equal(ResultsEntry.Rematch, _choices.Press(0, MenuAction.Confirm));
    }

    [Fact]
    public void PressesFromNobodyAreIgnored()
    {
        Assert.Null(_choices.Press(-1, MenuAction.Confirm));
        Assert.Null(_choices.Press(2, MenuAction.Confirm));
    }

    [Fact]
    public void BeginStartsOver()
    {
        _choices.Press(0, MenuAction.Confirm);
        _choices.Press(1, MenuAction.Down);

        _choices.Begin([ResultsEntry.Rematch, ResultsEntry.MainMenu], choosers: 2);

        Assert.False(_choices.IsIn(0));
        Assert.Equal(0, _choices.CursorOf(1));
    }
}
