using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Edits a map field (<see cref="SchemaFieldKind.Map"/>): an object whose keys the file chooses, such as a character's
/// <c>moveRoles</c> (role → move id). Only what keeps the object well-formed is checked; Sim.Data's validation reports
/// unknown keys and bad values like any other problem.
/// </summary>
public static class JsonMap
{
    /// <summary>The map's entries as fields of <paramref name="value"/>'s kind, in file order, so a form can edit them.</summary>
    public static SchemaObject EntriesOf(JsonObject map, SchemaField value)
    {
        List<SchemaField> entries = [];
        foreach (KeyValuePair<string, JsonNode?> entry in map)
        {
            entries.Add(value with { Name = entry.Key, IsRequired = false });
        }

        return new SchemaObject(entries);
    }

    /// <summary>Why <paramref name="key"/> can't be added; null when it can.</summary>
    public static string? AddProblem(JsonObject map, string key)
    {
        if (key.Trim().Length == 0)
        {
            return "Type a key first.";
        }

        return map.ContainsKey(key) ? $"'{key}' is already there." : null;
    }

    /// <summary>Adds <paramref name="key"/> last, with <paramref name="value"/>'s default (<see cref="SchemaDefaults"/>).</summary>
    public static void Add(JsonObject map, string key, SchemaField value) =>
        map.Add(key, SchemaDefaults.ValueFor(value));
}
