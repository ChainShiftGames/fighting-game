using System;
using FightingGame.UI.Framework;

namespace FightingGame.UI.CharacterSelect;

/// <summary>
/// The portrait grid's slots: one per character in the game data's order, then the random slot. Moving off an edge
/// wraps to the other side.
/// </summary>
public sealed class CharacterGrid
{
    public CharacterGrid(int characterCount, int columns)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(characterCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        CharacterCount = characterCount;
        Columns = Math.Min(columns, SlotCount);
    }

    public int CharacterCount { get; }

    public int Columns { get; }

    public int SlotCount => CharacterCount + 1;

    public int RandomSlot => CharacterCount;

    public int Rows => (SlotCount + Columns - 1) / Columns;

    public bool IsRandom(int slot) => slot == RandomSlot;

    /// <summary>The slot one step from <paramref name="slot"/> in a direction; other actions leave it.</summary>
    public int Move(int slot, MenuAction direction)
    {
        int row = slot / Columns;
        int column = slot % Columns;
        if ((direction & MenuAction.Left) != 0)
        {
            return slot == 0 ? SlotCount - 1 : slot - 1;
        }

        if ((direction & MenuAction.Right) != 0)
        {
            return slot == SlotCount - 1 ? 0 : slot + 1;
        }

        if ((direction & (MenuAction.Up | MenuAction.Down)) == 0 || Rows == 1)
        {
            return slot;
        }

        // Up and down keep the column; on the short last row a column without a slot goes to its last one.
        int step = (direction & MenuAction.Up) != 0 ? -1 : 1;
        int next = ((row + step + Rows) % Rows * Columns) + column;
        return Math.Min(next, SlotCount - 1);
    }
}
