using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Adds keys the schema allows without listing them (<see cref="SchemaObject.Extra"/>), such as a character's move
/// roles or its mechanic's config keys. Only what keeps the object well-formed is checked; Sim.Data's validation
/// reports unknown keys and bad values like any other problem.
/// </summary>
public static class JsonMap
{
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
