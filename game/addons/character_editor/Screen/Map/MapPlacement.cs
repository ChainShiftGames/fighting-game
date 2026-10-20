#if TOOLS
using System;
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Map;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Map;

/// <summary>
/// Where the map draws each column heading, card and category box, in 1× design pixels before pan and zoom:
/// columns left to right, cards top to bottom, a jump card a little apart below its column.
/// </summary>
public sealed class MapPlacement
{
    public const float CardWidth = 176f;
    public const float CardHeight = 78f;
    private const float Left = 28f;
    private const float Top = 82f;
    private const float HeaderTop = 18f;
    private const float ColumnPitch = 236f;
    private const float RowPitch = 126f;
    private const float JumpGap = 22f;
    private const float GroupPadX = 12f;
    private const float GroupPadTop = 16f;
    private const float GroupPadBottom = 14f;

    private readonly Dictionary<string, Rect2> _cards = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Rect2> _groups = new(StringComparer.Ordinal);
    private readonly List<(MapColumn Column, Vector2 At)> _headers = [];

    /// <param name="categories">Each shown move's category, for arrows into a category without a box.</param>
    public MapPlacement(MoveMapLayout layout, IReadOnlyDictionary<string, string> categories)
    {
        Layout = layout;
        Categories = categories;
        for (int column = 0; column < layout.Columns.Count; column++)
        {
            float x = Left + (column * ColumnPitch);
            _headers.Add((layout.Columns[column], new Vector2(x, HeaderTop)));
            float y = Top;
            foreach (string entry in layout.Columns[column].Entries)
            {
                y += entry == MoveMapLayout.JumpEntry ? JumpGap : 0f;
                _cards[entry] = new Rect2(x, y, CardWidth, CardHeight);
                y += RowPitch;
            }
        }

        foreach (MapGroup group in layout.Groups)
        {
            Rect2? box = null;
            foreach (string move in group.Moves)
            {
                box = box is Rect2 so ? so.Merge(_cards[move]) : _cards[move];
            }

            if (box is Rect2 area)
            {
                _groups[group.Category] = new Rect2(
                    area.Position - new Vector2(GroupPadX, GroupPadTop),
                    area.Size + new Vector2(2f * GroupPadX, GroupPadTop + GroupPadBottom));
            }
        }
    }

    public MoveMapLayout Layout { get; }

    public IReadOnlyDictionary<string, string> Categories { get; }

    public IReadOnlyDictionary<string, Rect2> Cards => _cards;

    /// <summary>Boxes around categories that moves chain into as a whole, by category.</summary>
    public IReadOnlyDictionary<string, Rect2> Groups => _groups;

    public IReadOnlyList<(MapColumn Column, Vector2 At)> Headers => _headers;

    /// <summary>Everything drawn, for fitting the view.</summary>
    public Rect2 Bounds
    {
        get
        {
            var bounds = new Rect2(0f, 0f, Left + CardWidth, Top + CardHeight);
            foreach (Rect2 card in _cards.Values)
            {
                bounds = bounds.Merge(card);
            }

            foreach (Rect2 group in _groups.Values)
            {
                bounds = bounds.Merge(group);
            }

            return bounds.Grow(Left);
        }
    }
}
#endif
