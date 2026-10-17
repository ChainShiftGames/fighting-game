using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Adds and removes a move's cancel windows. A window's frames and flags are edited in place on its object; its
/// targets (<c>into</c>, <c>intoCategories</c>) through <see cref="JsonStringList"/>.
/// </summary>
public static class MoveCancels
{
    /// <summary>What a new window cancels on until the user changes it: the case every gatling route has.</summary>
    public const string DefaultOn = "hit";

    /// <summary>The move's cancel windows; entries that aren't objects are skipped (validation reports them).</summary>
    public static IReadOnlyList<JsonObject> Windows(JsonObject move)
    {
        List<JsonObject> windows = [];
        foreach (JsonNode? window in move[MoveFields.Cancels] as JsonArray ?? [])
        {
            if (window is JsonObject obj)
            {
                windows.Add(obj);
            }
        }

        return windows;
    }

    /// <summary>The moves a window can cancel into: the character's attacks, in move list order.</summary>
    public static IReadOnlyList<string> TargetsOf(CharacterDocument document)
    {
        List<string> targets = [];
        foreach (string id in document.MoveIds)
        {
            if (document.Move(id) is EditedFile move && !targets.Contains(id)
                && JsonNodeValue.Text(move.Root[MoveFields.Category]) != MoveFields.BasicCategory)
            {
                targets.Add(id);
            }
        }

        return targets;
    }

    /// <summary>
    /// Adds a window from the first active frame to the end of the move (the whole move for a basic move), on hit,
    /// with no targets yet: validation asks for one until the user picks it.
    /// </summary>
    /// <param name="fieldOrder">The move's field names in schema order, for a move without cancels.</param>
    public static JsonObject Add(JsonObject move, IReadOnlyList<string> fieldOrder)
    {
        FramePhases? phases = FramePhases.Of(move);
        int last = phases?.TotalFrames ?? 1;
        int first = phases is { IsBasic: false } ? phases.FirstActiveFrame : 1;
        JsonObject window = new()
        {
            [MoveFields.FirstFrame] = first,
            [MoveFields.LastFrame] = last,
            [MoveFields.On] = new JsonArray(DefaultOn),
        };

        if (move[MoveFields.Cancels] is not JsonArray list)
        {
            list = [];
            JsonFieldOrder.Set(move, MoveFields.Cancels, list, fieldOrder);
        }

        list.Add(window);
        return window;
    }

    /// <summary>Removes the window; the <c>cancels</c> field goes too once it is empty.</summary>
    public static void Remove(JsonObject move, JsonObject window)
    {
        if (move[MoveFields.Cancels] is not JsonArray list)
        {
            return;
        }

        list.Remove(window);
        if (list.Count == 0)
        {
            move.Remove(MoveFields.Cancels);
        }
    }
}
