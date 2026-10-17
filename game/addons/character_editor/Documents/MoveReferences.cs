using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Where a character's data names a move by id outside the move list: its <c>states</c> and other moves' cancel
/// targets. Deleting the move breaks these, so the editor shows them before it does; validation reports any left.
/// </summary>
public static class MoveReferences
{
    private const string StatesField = "states";

    /// <summary>Each place as <c>file: field</c>, e.g. <c>5a.json: cancel 1 into</c>.</summary>
    public static IReadOnlyList<string> Find(CharacterDocument document, string moveId)
    {
        List<string> places = [];
        string characterFile = FileName(document.Character.Path);
        foreach (KeyValuePair<string, JsonNode?> state in document.Character.Root[StatesField] as JsonObject ?? [])
        {
            if (JsonNodeValue.Text(state.Value) == moveId)
            {
                places.Add($"{characterFile}: {StatesField}.{state.Key}");
            }
        }

        // A move listed twice (validation reports it) is still one file.
        HashSet<string> seen = new(StringComparer.Ordinal) { moveId };
        foreach (string otherId in document.MoveIds)
        {
            if (seen.Add(otherId) && document.Move(otherId) is EditedFile other)
            {
                AddCancelTargets(other, moveId, places);
            }
        }

        return places;
    }

    private static void AddCancelTargets(EditedFile move, string moveId, List<string> places)
    {
        IReadOnlyList<JsonObject> windows = MoveCancels.Windows(move.Root);
        for (int i = 0; i < windows.Count; i++)
        {
            if (JsonStringList.Contains(windows[i], MoveFields.Into, moveId))
            {
                places.Add($"{FileName(move.Path)}: cancel {i + 1} {MoveFields.Into}");
            }
        }
    }

    private static string FileName(string path) => path[(path.LastIndexOf('/') + 1)..];
}
