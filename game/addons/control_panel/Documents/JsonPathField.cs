using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>
/// Reads and rewrites one value anywhere in a JSON file's text, found by a path such as <c>stats.gravity</c> or
/// <c>hits[0].hitstop</c>.
/// </summary>
/// <remarks>
/// Rewrites only the value's own characters, so the file keeps its exact layout and key order and the git diff shows
/// just the value. Validation stays with the file's loader.
/// </remarks>
public static class JsonPathField
{
    private static readonly JsonReaderOptions _options = new() { CommentHandling = JsonCommentHandling.Skip };

    /// <exception cref="FormatException">There is no number at the path.</exception>
    public static double ReadNumber(string json, string path)
    {
        string literal = ReadLiteral(json, path, JsonTokenType.Number);
        return double.Parse(literal, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <exception cref="FormatException">There is no true or false at the path.</exception>
    public static bool ReadBool(string json, string path)
    {
        string literal = ReadLiteral(json, path, JsonTokenType.True, JsonTokenType.False);
        return literal == "true";
    }

    /// <exception cref="FormatException">There is no string at the path.</exception>
    public static string ReadString(string json, string path) =>
        JsonSerializer.Deserialize<string>(ReadLiteral(json, path, JsonTokenType.String))!;

    /// <summary>True when the path holds a value of any kind.</summary>
    public static bool Has(string json, string path) => Find(Encoding.UTF8.GetBytes(json), path).Length > 0;

    /// <summary>The text with the number at <paramref name="path"/> replaced.</summary>
    /// <param name="decimals">Digits after the point to keep at most; 0 writes a whole number.</param>
    /// <exception cref="FormatException">There is no number at the path.</exception>
    public static string WriteNumber(string json, string path, double value, int decimals) =>
        Replace(json, path, NumberText(value, decimals), JsonTokenType.Number);

    /// <exception cref="FormatException">There is no true or false at the path.</exception>
    public static string WriteBool(string json, string path, bool value) =>
        Replace(json, path, value ? "true" : "false", JsonTokenType.True, JsonTokenType.False);

    /// <exception cref="FormatException">There is no string at the path.</exception>
    public static string WriteString(string json, string path, string value) =>
        Replace(json, path, JsonSerializer.Serialize(value), JsonTokenType.String);

    /// <summary>How a number is written: invariant, without trailing zeros, e.g. 3, 0.5, 1.25.</summary>
    public static string NumberText(double value, int decimals)
    {
        double rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        return decimals == 0
            ? ((long)rounded).ToString(CultureInfo.InvariantCulture)
            : rounded.ToString("0." + new string('#', decimals), CultureInfo.InvariantCulture);
    }

    private static string ReadLiteral(string json, string path, params JsonTokenType[] kinds)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        ValueSpan span = Find(bytes, path);
        Check(span, path, kinds);
        return Encoding.UTF8.GetString(bytes, span.Start, span.Length);
    }

    private static string Replace(string json, string path, string literal, params JsonTokenType[] kinds)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        ValueSpan span = Find(bytes, path);
        Check(span, path, kinds);
        return string.Concat(
            Encoding.UTF8.GetString(bytes, 0, span.Start),
            literal,
            Encoding.UTF8.GetString(bytes, span.Start + span.Length, bytes.Length - span.Start - span.Length));
    }

    private static void Check(ValueSpan span, string path, JsonTokenType[] kinds)
    {
        if (span.Length == 0)
        {
            throw new FormatException($"'{path}' isn't in the file.");
        }

        if (Array.IndexOf(kinds, span.Kind) < 0)
        {
            throw new FormatException($"'{path}' is a {span.Kind}, not a {kinds[0]}.");
        }
    }

    // Walks the tokens keeping the path of each value; the match is the first value whose path equals the target.
    private static ValueSpan Find(byte[] bytes, string path)
    {
        string[] target = Segments(path);
        var reader = new Utf8JsonReader(bytes, _options);
        var current = new List<string>();
        var nextIndex = new Stack<int>();
        string? property = null;
        while (reader.Read())
        {
            JsonTokenType token = reader.TokenType;
            if (token == JsonTokenType.PropertyName)
            {
                property = reader.GetString();
                continue;
            }

            if (token is JsonTokenType.EndObject or JsonTokenType.EndArray)
            {
                nextIndex.Pop();
                if (current.Count > 0)
                {
                    current.RemoveAt(current.Count - 1);
                }

                continue;
            }

            bool root = nextIndex.Count == 0;
            if (!root)
            {
                int index = nextIndex.Pop();
                current.Add(index >= 0 ? $"[{index}]" : property!);
                nextIndex.Push(index >= 0 ? index + 1 : -1);
            }

            if (Same(current, target))
            {
                long start = reader.TokenStartIndex;
                if (token is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }

                return new ValueSpan((int)start, (int)(reader.BytesConsumed - start), token);
            }

            if (token is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                nextIndex.Push(token == JsonTokenType.StartArray ? 0 : -1);
            }
            else if (!root)
            {
                current.RemoveAt(current.Count - 1);
            }
        }

        return default;
    }

    /// <summary>A path's steps: <c>hits[0].hitstop</c> → <c>hits</c>, <c>[0]</c>, <c>hitstop</c>.</summary>
    public static string[] Segments(string path)
    {
        var segments = new List<string>();
        foreach (string part in path.Split('.'))
        {
            int bracket = part.IndexOf('[', StringComparison.Ordinal);
            if (bracket < 0)
            {
                segments.Add(part);
                continue;
            }

            if (bracket > 0)
            {
                segments.Add(part[..bracket]);
            }

            segments.Add(part[bracket..]);
        }

        return [.. segments];
    }

    private static bool Same(List<string> current, string[] target)
    {
        if (current.Count != target.Length)
        {
            return false;
        }

        for (int i = 0; i < target.Length; i++)
        {
            if (!string.Equals(current[i], target[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private readonly record struct ValueSpan(int Start, int Length, JsonTokenType Kind);
}
