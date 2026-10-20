using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>
/// Which moves the map shows for a <see cref="MapLens"/> and in which column, top to bottom: normals by button
/// (<see cref="MoveMapSettings.NormalColumns"/>), then specials and supers, then for <see cref="MapLens.Everything"/>
/// throws, movement and reactions. A jump-cancel target and boxes around categories that moves chain into come
/// with it. Pixel positions are the canvas's business.
/// </summary>
public sealed class MoveMapLayout
{
    /// <summary>The column entry for "a jump": not a move id (ids can't hold brackets).</summary>
    public const string JumpEntry = "(jump)";

    private static readonly string[] _groupedCategories = ["special", "super", "throw"];

    private MoveMapLayout(IReadOnlyList<MapColumn> columns, IReadOnlyList<MapGroup> groups)
    {
        Columns = columns;
        Groups = groups;
    }

    public IReadOnlyList<MapColumn> Columns { get; }

    /// <summary>Categories some shown move chains into as a whole, with their shown members (two or more).</summary>
    public IReadOnlyList<MapGroup> Groups { get; }

    public static MoveMapLayout Of(
        CharacterDocument document, MoveMapSettings settings, MapLens lens, IReadOnlyList<ChainLink> links)
    {
        OrderedMoves moves = Moves(document);
        List<MapColumn> columns = [];
        if (lens != MapLens.SpecialMoves)
        {
            AddNormalColumns(columns, moves, settings);
        }

        AddColumn(columns, "Specials & super", "button + motion", OfCategory(moves, "special", "super"));
        if (lens == MapLens.Everything)
        {
            AddColumn(columns, "Throws", "grab up close", OfCategory(moves, "throw"));
            AddBasicColumns(columns, document, moves, settings);
        }

        HashSet<string> shown = new(StringComparer.Ordinal);
        foreach (MapColumn column in columns)
        {
            shown.UnionWith(column.Entries);
        }

        if (lens != MapLens.SpecialMoves)
        {
            AddJump(columns, links, shown);
        }

        return new MoveMapLayout(columns, ChainedGroups(moves, links, shown));
    }

    /// <summary>The column and row of an entry; null when this lens doesn't show it.</summary>
    public (int Column, int Row)? Find(string entry)
    {
        for (int column = 0; column < Columns.Count; column++)
        {
            int row = IndexOf(Columns[column].Entries, entry);
            if (row >= 0)
            {
                return (column, row);
            }
        }

        return null;
    }

    // In move list order (input priority), each move once; a listed move without a file isn't shown.
    private static OrderedMoves Moves(CharacterDocument document)
    {
        OrderedMoves moves = new();
        foreach (string id in document.MoveIds)
        {
            if (document.Move(id) is EditedFile move && moves.TryAdd(id, move.Root))
            {
                moves.Order.Add(id);
            }
        }

        return moves;
    }

    private static void AddNormalColumns(
        List<MapColumn> columns, OrderedMoves moves, MoveMapSettings settings)
    {
        List<string> normals = OfCategory(moves, "normal");
        HashSet<string> placed = new(StringComparer.Ordinal);
        foreach (MapColumnSetting setting in settings.NormalColumns)
        {
            List<string> entries = normals.FindAll(id => !placed.Contains(id) && UsesOnly(moves[id], setting.Buttons));
            entries.Sort((first, second) => CompareNormals(moves[first], moves[second], setting.Buttons));
            placed.UnionWith(entries);
            AddColumn(columns, setting.Title, setting.Hint, entries);
        }

        AddColumn(columns, "Other normals", "several buttons at once", normals.FindAll(id => !placed.Contains(id)));
    }

    private static void AddBasicColumns(
        List<MapColumn> columns, CharacterDocument document, OrderedMoves moves, MoveMapSettings settings)
    {
        List<string> movement = [];
        List<string> reactions = [];
        foreach (KeyValuePair<string, JsonNode?> state in document.Character.Root["states"] as JsonObject ?? [])
        {
            if (JsonNodeValue.Text(state.Value) is string id && moves.ContainsKey(id)
                && !movement.Contains(id) && !reactions.Contains(id))
            {
                (IndexOf(settings.MovementStates, state.Key) >= 0 ? movement : reactions).Add(id);
            }
        }

        List<string> other = OfCategory(moves, MoveFields.BasicCategory)
            .FindAll(id => !movement.Contains(id) && !reactions.Contains(id));
        AddColumn(columns, "Movement", "walking, jumping, dashing", movement);
        AddColumn(columns, "Reactions", "getting hit, blocking", reactions);
        AddColumn(columns, "Other", "not used by a state", other);
    }

    private static void AddJump(List<MapColumn> columns, IReadOnlyList<ChainLink> links, HashSet<string> shown)
    {
        foreach (ChainLink link in links)
        {
            if (link.Kind != ChainTargetKind.Jump || !shown.Contains(link.From))
            {
                continue;
            }

            for (int i = 0; i < columns.Count; i++)
            {
                if (IndexOf(columns[i].Entries, link.From) >= 0)
                {
                    columns[i] = columns[i] with { Entries = [.. columns[i].Entries, JumpEntry] };
                    return;
                }
            }
        }
    }

    private static List<MapGroup> ChainedGroups(
        OrderedMoves moves, IReadOnlyList<ChainLink> links, HashSet<string> shown)
    {
        List<MapGroup> groups = [];
        foreach (string category in _groupedCategories)
        {
            bool targeted = false;
            foreach (ChainLink link in links)
            {
                targeted |= link.Kind == ChainTargetKind.Category && link.Target == category && shown.Contains(link.From);
            }

            List<string> members = OfCategory(moves, category).FindAll(shown.Contains);
            if (targeted && members.Count >= 2)
            {
                groups.Add(new MapGroup(category, members));
            }
        }

        return groups;
    }

    private static List<string> OfCategory(OrderedMoves moves, params string[] categories)
    {
        List<string> ids = [];
        foreach (string category in categories)
        {
            ids.AddRange(moves.Order.FindAll(id => JsonNodeValue.Text(moves[id][MoveFields.Category]) == category));
        }

        return ids;
    }

    private static void AddColumn(List<MapColumn> columns, string title, string hint, List<string> entries)
    {
        if (entries.Count > 0)
        {
            columns.Add(new MapColumn(title, hint, entries));
        }
    }

    // Every button the move is pressed with is one of the column's (a press of several, like a burst, rarely is).
    private static bool UsesOnly(JsonObject move, IReadOnlyList<string> buttons)
    {
        IReadOnlyList<string> pressed = MoveInput.Buttons(move);
        foreach (string button in pressed)
        {
            if (IndexOf(buttons, button) < 0)
            {
                return false;
            }
        }

        return pressed.Count > 0;
    }

    // By the column's button order, then ground before air, standing (5) before crouching (2) before the rest.
    private static int CompareNormals(JsonObject first, JsonObject second, IReadOnlyList<string> buttons)
    {
        int byButton = ButtonRank(first, buttons).CompareTo(ButtonRank(second, buttons));
        return byButton != 0 ? byButton : StanceRank(first).CompareTo(StanceRank(second));
    }

    private static int ButtonRank(JsonObject move, IReadOnlyList<string> buttons)
    {
        IReadOnlyList<string> pressed = MoveInput.Buttons(move);
        return pressed.Count > 0 && IndexOf(buttons, pressed[0]) is int rank and >= 0 ? rank : buttons.Count;
    }

    private static int StanceRank(JsonObject move)
    {
        int air = JsonNodeValue.Text(move[MoveFields.UsableIn]) == "air" ? 10 : 0;
        return air + MoveInput.Motion(move) switch
        {
            "5" => 0,
            "2" => 1,
            _ => 2,
        };
    }

    private static int IndexOf(IReadOnlyList<string> items, string item)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == item)
            {
                return i;
            }
        }

        return -1;
    }

    private sealed class OrderedMoves : Dictionary<string, JsonObject>
    {
        public OrderedMoves()
            : base(StringComparer.Ordinal)
        {
        }

        public List<string> Order { get; } = [];
    }
}
