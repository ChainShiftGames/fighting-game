using System;
using System.Collections.Generic;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>
/// Spreads cards of different heights over side-by-side columns so the columns end as evenly as possible, and keeps
/// the cards in each column in their original order so related settings stay near each other.
/// </summary>
/// <remarks>
/// Tallest first, each into the shortest column so far (longest-processing-time packing): close to even for the
/// handful of cards a page has. Engine-free so it can be tested.
/// </remarks>
public static class ColumnPacking
{
    /// <summary>How many columns of at least <paramref name="minimumWidth"/> fit, between 1 and <paramref name="maximum"/>.</summary>
    public static int ColumnsFor(float width, float minimumWidth, float spacing, int maximum)
    {
        int columns = (int)((width + spacing) / (minimumWidth + spacing));
        return Math.Clamp(columns, 1, maximum);
    }

    /// <returns>Per column, the indexes of its cards in their original order.</returns>
    public static List<int>[] Pack(IReadOnlyList<float> heights, int columns)
    {
        var order = new List<int>(heights.Count);
        for (int i = 0; i < heights.Count; i++)
        {
            order.Add(i);
        }

        // Stable: equal heights keep their order, so the result never flips between two equal layouts.
        order.Sort((a, b) => heights[b].CompareTo(heights[a]) is int byHeight and not 0 ? byHeight : a.CompareTo(b));
        var packed = new List<int>[Math.Max(1, columns)];
        var filled = new float[packed.Length];
        for (int column = 0; column < packed.Length; column++)
        {
            packed[column] = [];
        }

        foreach (int card in order)
        {
            int shortest = 0;
            for (int column = 1; column < packed.Length; column++)
            {
                if (filled[column] < filled[shortest])
                {
                    shortest = column;
                }
            }

            packed[shortest].Add(card);
            filled[shortest] += heights[card];
        }

        foreach (List<int> column in packed)
        {
            column.Sort();
        }

        // The column holding the first card goes left, and so on, so a page still reads from its top-left card.
        Array.Sort(packed, (a, b) => First(a).CompareTo(First(b)));
        return packed;
    }

    private static int First(List<int> column) => column.Count > 0 ? column[0] : int.MaxValue;
}
