using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>
/// Which moves can interrupt into which, read from every move's cancel windows: named moves (<c>into</c>), whole
/// categories (<c>intoCategories</c>) and jump cancels. One link per source and target, however many windows name it.
/// </summary>
public static class MoveChains
{
    private const string IntoJump = "intoJump";

    /// <summary>Every link of the character's moves, in move list order.</summary>
    public static IReadOnlyList<ChainLink> Of(CharacterDocument document)
    {
        List<ChainLink> links = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string id in document.MoveIds)
        {
            if (seen.Add(id) && document.Move(id) is EditedFile move)
            {
                links.AddRange(From(id, move.Root));
            }
        }

        return links;
    }

    /// <summary>The move's own links.</summary>
    public static IReadOnlyList<ChainLink> From(string id, JsonObject move)
    {
        List<ChainLink> links = [];
        foreach (JsonObject window in MoveCancels.Windows(move))
        {
            foreach (string target in JsonStringList.Read(window, MoveFields.Into))
            {
                Add(links, new ChainLink(id, ChainTargetKind.Move, target));
            }

            foreach (string category in JsonStringList.Read(window, MoveFields.IntoCategories))
            {
                Add(links, new ChainLink(id, ChainTargetKind.Category, category));
            }

            if (JsonNodeValue.Flag(window[IntoJump]) == true)
            {
                Add(links, new ChainLink(id, ChainTargetKind.Jump, ""));
            }
        }

        return links;
    }

    private static void Add(List<ChainLink> links, ChainLink link)
    {
        if (!links.Contains(link))
        {
            links.Add(link);
        }
    }
}
