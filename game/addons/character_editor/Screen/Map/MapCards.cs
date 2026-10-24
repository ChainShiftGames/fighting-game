#if TOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;
using Sim.Data.Validation;

namespace FightingGame.Addons.CharacterEditor.Screen.Map;

/// <summary>Builds the <see cref="MapCard"/> of every entry a <see cref="MoveMapLayout"/> shows.</summary>
public static class MapCards
{
    public static Dictionary<string, MapCard> Of(CharacterDocument document, MoveMapLayout layout,
        MoveMapSettings settings, IReadOnlyList<DataError> problems)
    {
        Dictionary<string, MapCard> cards = new(StringComparer.Ordinal);
        IReadOnlyList<string> moveIds = document.MoveIds;
        foreach (MapColumn column in layout.Columns)
        {
            foreach (string entry in column.Entries)
            {
                if (entry == MoveMapLayout.JumpEntry)
                {
                    cards[entry] = new MapCard(entry, "Jump", "cancels into the air", "↑", [], [], [], false,
                        "Holding up during the window starts a jump");
                }
                else if (document.Move(entry) is EditedFile move)
                {
                    cards[entry] = Card(entry, move, settings, problems, moveIds);
                }
            }
        }

        return cards;
    }

    private static MapCard Card(string id, EditedFile move, MoveMapSettings settings,
        IReadOnlyList<DataError> problems, IReadOnlyList<string> moveIds)
    {
        JsonObject root = move.Root;
        MoveFacts facts = MoveFacts.Of(root);
        int problemCount = 0;
        foreach (DataError problem in problems)
        {
            problemCount += problem.File == move.Path ? 1 : 0;
        }

        int priority = IndexOf(moveIds, id) + 1;
        string tooltip = string.Create(CultureInfo.InvariantCulture,
            $"{id} · input priority {priority} of {moveIds.Count}\n{move.Path}");
        if (problemCount > 0)
        {
            tooltip += string.Create(CultureInfo.InvariantCulture, $"\n{problemCount} problem(s): see the status chip");
        }

        return new MapCard(
            id,
            MoveNames.Of(id, root, settings),
            Meta(root, facts),
            MoveGlyphs.Arrows(MoveInput.Motion(root)),
            MoveGlyphs.Keycaps(MoveInput.Buttons(root)),
            Tagged(root),
            facts.Phases?.Lengths ?? [],
            problemCount > 0,
            tooltip);
    }

    private static string Meta(JsonObject move, MoveFacts facts)
    {
        string frames = facts.Phases is FramePhases phases
            ? string.Create(CultureInfo.InvariantCulture, $"{phases.TotalFrames} f")
            : "frames unreadable";
        string category = JsonNodeValue.Text(move[MoveFields.Category]) ?? "";
        if (facts.Damage is not int damage)
        {
            return frames;
        }

        string dealt = string.Create(CultureInfo.InvariantCulture, $"{damage} dmg");
        return facts.HitsFromProjectile ? $"projectile · {dealt}"
            : category == "throw" ? $"throw · {dealt}"
            : $"{dealt} · {frames}";
    }

    // Only guards that change how to block it: mid is the default, and a throw is never blocked anyway.
    private static List<string> Tagged(JsonObject move)
    {
        List<string> tagged = [];
        if (JsonNodeValue.Text(move[MoveFields.Category]) == "throw")
        {
            return tagged;
        }

        foreach (string guard in MoveGuards.Distinct(move))
        {
            if (guard != "mid")
            {
                tagged.Add(guard);
            }
        }

        return tagged;
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
}
#endif
