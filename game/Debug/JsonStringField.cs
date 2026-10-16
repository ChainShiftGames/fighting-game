using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FightingGame.Debug;

/// <summary>Reads and rewrites one top-level string field of a JSON data file's text.</summary>
/// <remarks>
/// Like <see cref="JsonNumberField"/>: rewrites only the string, so the file keeps its exact layout and key order.
/// Validation stays with Sim.Data, which reloads the file afterwards.
/// </remarks>
public static class JsonStringField
{
    /// <exception cref="FormatException">The field is missing or isn't a string.</exception>
    public static string Read(string json, string key)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty(key, out JsonElement field) || field.ValueKind != JsonValueKind.String)
        {
            throw new FormatException($"'{key}' isn't a string at the top level.");
        }

        return field.GetString()!;
    }

    /// <exception cref="FormatException">The field is missing, not a string, or appears more than once.</exception>
    public static string Write(string json, string key, string value)
    {
        Read(json, key);
        var field = new Regex($@"(""{Regex.Escape(key)}""\s*:\s*)""(?:[^""\\]|\\.)*""");
        MatchCollection matches = field.Matches(json);
        if (matches.Count != 1)
        {
            throw new FormatException($"'{key}' appears {matches.Count} times; expected exactly once.");
        }

        Match match = matches[0];
        string text = JsonSerializer.Serialize(value);
        return string.Concat(json.AsSpan(0, match.Index), match.Groups[1].Value, text, json.AsSpan(match.Index + match.Length));
    }
}
