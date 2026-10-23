using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Framework;
using Xunit;

namespace FightingGame.Tests.UI.CharacterSelect;

public sealed class CharacterGridTests
{
    [Fact]
    public void TheRandomSlotComesAfterTheLastCharacter()
    {
        var grid = new CharacterGrid(characterCount: 3, columns: 6);

        Assert.Equal(4, grid.SlotCount);
        Assert.True(grid.IsRandom(3));
        Assert.Equal(4, grid.Columns);
    }

    [Fact]
    public void LeftAndRightWrapAroundEveryRow()
    {
        var grid = new CharacterGrid(characterCount: 4, columns: 3);

        Assert.Equal(4, grid.Move(0, MenuAction.Left));
        Assert.Equal(0, grid.Move(4, MenuAction.Right));
        Assert.Equal(3, grid.Move(2, MenuAction.Right));
    }

    [Fact]
    public void UpAndDownKeepTheColumnAndWrap()
    {
        // 0 1 2
        // 3 4
        var grid = new CharacterGrid(characterCount: 4, columns: 3);

        Assert.Equal(4, grid.Move(1, MenuAction.Down));
        Assert.Equal(1, grid.Move(4, MenuAction.Down));
        Assert.Equal(3, grid.Move(0, MenuAction.Up));
        Assert.Equal(4, grid.Move(2, MenuAction.Down));
    }

    [Fact]
    public void OneRowIgnoresUpAndDown()
    {
        var grid = new CharacterGrid(characterCount: 1, columns: 6);

        Assert.Equal(1, grid.Move(1, MenuAction.Up));
        Assert.Equal(0, grid.Move(0, MenuAction.Confirm));
    }
}
