using System.Collections.Generic;
using System.Linq;
using FightingGame.Addons.ControlPanel.Screen;
using Xunit;

namespace FightingGame.Tests.Addons.ControlPanel;

public sealed class ColumnPackingTests
{
    [Fact]
    public void ColumnsEndAsEvenlyAsTheCardsAllow()
    {
        // The Mechanics page: nine cards from 4 to 13 rows.
        float[] heights = [4, 13, 11, 4, 6, 10, 6, 9, 11];

        List<int>[] columns = ColumnPacking.Pack(heights, 3);

        float[] totals = columns.Select(column => column.Sum(card => heights[card])).ToArray();
        Assert.True(totals.Max() - totals.Min() <= 4, string.Join(", ", totals));
        Assert.Equal(Enumerable.Range(0, heights.Length), columns.SelectMany(column => column).Order());
    }

    [Fact]
    public void EachColumnKeepsItsCardsInOrderAndTheFirstCardStartsTopLeft()
    {
        float[] heights = [3, 9, 2, 8, 5];

        List<int>[] columns = ColumnPacking.Pack(heights, 2);

        Assert.All(columns, column => Assert.Equal(column.Order(), column));
        Assert.Equal(0, columns[0][0]);
    }

    [Fact]
    public void MoreColumnsThanCardsLeavesSomeEmpty()
    {
        List<int>[] columns = ColumnPacking.Pack([5f], 3);

        Assert.Equal([0], columns[0]);
        Assert.Empty(columns[1]);
    }

    [Theory]
    [InlineData(1900f, 4)]
    [InlineData(1300f, 3)]
    [InlineData(900f, 2)]
    [InlineData(300f, 1)]
    public void AsManyColumnsAsFit(float width, int columns) =>
        Assert.Equal(columns, ColumnPacking.ColumnsFor(width, 420f, 14f, 4));
}
