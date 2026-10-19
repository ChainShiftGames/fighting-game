using System;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FightingGame.Debug;

/// <summary>Reads and rewrites one top-level whole-number field of a JSON data file's text.</summary>
/// <remarks>
/// Rewrites only the number, so the file keeps its exact layout, comments and key order and the git diff shows just
/// the value. Validation stays with the file's loader, which reloads it afterwards.
/// </remarks>
public static class JsonNumberField
{
    // Art files carry // comments; data files have none, so skipping them changes nothing there.
    private static readonly JsonDocumentOptions _options = new() { CommentHandling = JsonCommentHandling.Skip };

    /// <exception cref="FormatException">The field is missing or isn't a whole number.</exception>
    public static int Read(string json, string key)
    {
        using JsonDocument document = JsonDocument.Parse(json, _options);
        if (!document.RootElement.TryGetProperty(key, out JsonElement field) || field.ValueKind != JsonValueKind.Number
            || !field.TryGetInt32(out int value))
        {
            throw new FormatException($"'{key}' isn't a whole number at the top level.");
        }

        return value;
    }

    /// <exception cref="FormatException">The field is missing, not a number, or appears more than once.</exception>
    public static string Write(string json, string key, int value)
    {
        Read(json, key);
        var field = new Regex($"(\"{Regex.Escape(key)}\"\\s*:\\s*)-?[0-9]+");
        MatchCollection matches = field.Matches(json);
        if (matches.Count != 1)
        {
            throw new FormatException($"'{key}' appears {matches.Count} times; expected exactly once.");
        }

        Match match = matches[0];
        string number = value.ToString(CultureInfo.InvariantCulture);
        return string.Concat(json.AsSpan(0, match.Index), match.Groups[1].Value, number, json.AsSpan(match.Index + match.Length));
    }
}
