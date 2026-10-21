using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using FightingGame.Presentation.World.Sparks;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Rewrites one move's hit spark settings in an art file's text (<c>sparks</c>, read by <see cref="HitSparkLooks"/>).
/// Only that move's entry changes, written on one line, so the file keeps its comments and other entries. A move whose
/// hits all use the defaults loses its entry (with the comment lines just above it); a new one goes at the end.
/// </summary>
public static class ArtSparkText
{
    private const string SparksKey = "sparks";
    private const string Indent = "  ";

    private static readonly JsonReaderOptions _reader = new() { CommentHandling = JsonCommentHandling.Skip };

    /// <param name="sparks">One per hit, in hit order; trailing hits with default settings are left out.</param>
    /// <exception cref="FormatException">The text isn't a JSON object.</exception>
    public static string Write(string json, string moveId, IReadOnlyList<HitSparkSettings> sparks)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        string newline = json.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string? value = Value(sparks);
        SparksLayout layout = Find(bytes, moveId);
        if (layout.Object is not { } sparksObject)
        {
            return value is null ? json : AddSparksObject(bytes, layout.RootLastEnd, moveId, value, newline);
        }

        int index = layout.Entries.FindIndex(entry => entry.IsMove);
        if (index >= 0)
        {
            Entry entry = layout.Entries[index];
            return value is null
                ? Remove(bytes, sparksObject, layout.Entries, index)
                : JsonTextEdits.Splice(bytes, entry.ValueStart, entry.ValueEnd, value);
        }

        if (value is null)
        {
            return json;
        }

        if (layout.Entries.Count > 0)
        {
            int lastEnd = layout.Entries[^1].ValueEnd;
            string indent = JsonTextEdits.IndentOfLine(bytes, layout.Entries[^1].NameStart);
            return JsonTextEdits.Splice(bytes, lastEnd, lastEnd, $",{newline}{indent}\"{moveId}\": {value}");
        }

        string outer = JsonTextEdits.IndentOfLine(bytes, sparksObject.Start);
        return JsonTextEdits.Splice(
            bytes, sparksObject.Start + 1, sparksObject.End, $"{newline}{outer}{Indent}\"{moveId}\": {value}{newline}{outer}");
    }

    // The entry's value, or null when every hit uses the defaults.
    private static string? Value(IReadOnlyList<HitSparkSettings> sparks)
    {
        int count = sparks.Count;
        while (count > 0 && sparks[count - 1] == new HitSparkSettings())
        {
            count--;
        }

        if (count == 0)
        {
            return null;
        }

        var hits = new List<string>();
        for (int i = 0; i < count; i++)
        {
            hits.Add(Hit(sparks[i]));
        }

        return $"[{string.Join(", ", hits)}]";
    }

    private static string Hit(HitSparkSettings spark)
    {
        var fields = new List<string>();
        if (spark.Angle is float angle)
        {
            fields.Add($"\"angle\": {Number(angle)}");
        }

        if (spark.OffsetX != 0f || spark.OffsetY != 0f)
        {
            fields.Add($"\"offset\": {{ \"x\": {Number(spark.OffsetX)}, \"y\": {Number(spark.OffsetY)} }}");
        }

        if (spark.Style is HitSparkStyle style)
        {
            fields.Add($"\"style\": \"{JsonNamingPolicy.CamelCase.ConvertName(style.ToString())}\"");
        }

        if (spark.Scale != 1f)
        {
            fields.Add($"\"scale\": {Number(spark.Scale)}");
        }

        return fields.Count == 0 ? "{}" : $"{{ {string.Join(", ", fields)} }}";
    }

    private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    // Removes the entry and the comment lines just above it. Entries each on their own lines lose those lines; an
    // entry sharing its line is cut out with its comma.
    private static string Remove(byte[] bytes, (int Start, int End) sparksObject, List<Entry> entries, int index)
    {
        Entry entry = entries[index];
        if (index + 1 < entries.Count)
        {
            int from = OwnLines(bytes, entry.NameStart) ? CommentsAbove(bytes, entry.NameStart) : entry.NameStart;
            int to = OwnLines(bytes, entries[index + 1].NameStart)
                ? CommentsAbove(bytes, entries[index + 1].NameStart)
                : entries[index + 1].NameStart;
            return JsonTextEdits.Splice(bytes, from, to, string.Empty);
        }

        int start = index > 0 ? entries[index - 1].ValueEnd : sparksObject.Start + 1;
        int end = index > 0 ? entry.ValueEnd : sparksObject.End;
        return JsonTextEdits.Splice(bytes, start, end, string.Empty);
    }

    private static bool OwnLines(byte[] bytes, int nameStart) =>
        Encoding.UTF8.GetString(bytes, JsonTextEdits.LineStart(bytes, nameStart), nameStart - JsonTextEdits.LineStart(bytes, nameStart))
            .Trim().Length == 0;

    // The start of the comment-only lines right above the line at nameStart, or that line's start.
    private static int CommentsAbove(byte[] bytes, int nameStart)
    {
        int lineStart = JsonTextEdits.LineStart(bytes, nameStart);
        while (lineStart > 0)
        {
            int previous = JsonTextEdits.LineStart(bytes, lineStart - 1);
            if (!Encoding.UTF8.GetString(bytes, previous, lineStart - previous).TrimStart().StartsWith("//", StringComparison.Ordinal))
            {
                break;
            }

            lineStart = previous;
        }

        return lineStart;
    }

    private static string AddSparksObject(byte[] bytes, int rootLastEnd, string moveId, string value, string newline)
    {
        if (rootLastEnd < 0)
        {
            throw new FormatException("the art file is an empty object");
        }

        string indent = JsonTextEdits.IndentOfLine(bytes, rootLastEnd);
        return JsonTextEdits.Splice(bytes, rootLastEnd, rootLastEnd,
            $",{newline}{indent}\"{SparksKey}\": {{{newline}{indent}{Indent}\"{moveId}\": {value}{newline}{indent}}}");
    }

    private static SparksLayout Find(byte[] bytes, string moveId)
    {
        var layout = new SparksLayout();
        try
        {
            var reader = new Utf8JsonReader(bytes, _reader);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                throw new FormatException("the art file isn't a JSON object");
            }

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                bool isSparks = reader.ValueTextEquals(SparksKey);
                reader.Read();
                if (isSparks && reader.TokenType == JsonTokenType.StartObject)
                {
                    int start = (int)reader.TokenStartIndex;
                    ReadEntries(ref reader, moveId, layout.Entries);
                    layout.Object = (start, (int)reader.TokenStartIndex);
                }
                else
                {
                    reader.Skip();
                }

                layout.RootLastEnd = (int)reader.BytesConsumed;
            }
        }
        catch (JsonException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        return layout;
    }

    private static void ReadEntries(ref Utf8JsonReader reader, string moveId, List<Entry> entries)
    {
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            int nameStart = (int)reader.TokenStartIndex;
            bool isMove = reader.ValueTextEquals(moveId);
            reader.Read();
            int valueStart = (int)reader.TokenStartIndex;
            reader.Skip();
            entries.Add(new Entry(nameStart, valueStart, (int)reader.BytesConsumed, isMove));
        }
    }

    private readonly record struct Entry(int NameStart, int ValueStart, int ValueEnd, bool IsMove);

    private sealed class SparksLayout
    {
        public (int Start, int End)? Object { get; set; }

        public List<Entry> Entries { get; } = [];

        // Where the root object's last value ends, for adding "sparks" after it; -1 for an empty object.
        public int RootLastEnd { get; set; } = -1;
    }
}
