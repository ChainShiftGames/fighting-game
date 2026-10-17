using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Writes a data file's JSON tree in the hand-written style of <c>data/</c> (2-space indent, short objects and lists
/// on one line, boxes one per line), so saving from the editor keeps diffs small and the files easy to read.
/// </summary>
/// <remarks>
/// Layout rules: the root object is always broken over lines. A list of objects or lists puts each item on its own
/// line. A list of plain values goes on one line when it fits, else it fills lines up to <see cref="LineWidth"/>; a
/// top-level one that was wrapped by hand keeps its line starts (<see cref="ListLineStarts"/>).
/// An object inside a list (a box) goes on one line when it has only plain values and fits; an object field (frame
/// data, input) when it has at most <see cref="MaxInlineFields"/> fields, no objects inside, and fits.
/// </remarks>
public static class JsonDataWriter
{
    /// <summary>Same as the C# line limit in <c>.editorconfig</c>.</summary>
    public const int LineWidth = 120;

    /// <summary>Longer objects (hit properties, stats) read better one field per line.</summary>
    public const int MaxInlineFields = 4;

    private const string Indent = "  ";

    // Keeps non-ASCII display names and characters such as '+' readable instead of \u escapes.
    private static readonly JsonSerializerOptions _valueOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>The file's text, '\n' line endings and a final newline.</summary>
    public static string Write(JsonNode root) => Write(root, ListLineStarts.None);

    /// <summary>The file's text, keeping the hand wrapping of its top-level lists of names.</summary>
    public static string Write(JsonNode root, ListLineStarts layout)
    {
        StringBuilder text = new();
        if (root is JsonObject obj)
        {
            WriteObject(text, obj, 0, layout);
        }
        else
        {
            WriteBroken(text, root, 0);
        }

        text.Append('\n');
        return text.ToString();
    }

    private static void WriteNode(StringBuilder text, JsonNode? node, int depth, int lineStart, bool isListItem)
    {
        int column = text.Length - lineStart;
        if (node is JsonObject or JsonArray && !FitsInline(node, column, isListItem))
        {
            WriteBroken(text, node, depth);
            return;
        }

        text.Append(Inline(node));
    }

    private static bool FitsInline(JsonNode node, int column, bool isListItem)
    {
        bool allowed = node switch
        {
            JsonArray array => !HasContainers(array),
            JsonObject obj when isListItem => HasOnlyValues(obj),
            JsonObject obj => obj.Count <= MaxInlineFields && !HasObjects(obj),
            _ => true,
        };

        // +1 for the comma that may follow.
        return allowed && column + Inline(node).Length + 1 <= LineWidth;
    }

    private static void WriteBroken(StringBuilder text, JsonNode? node, int depth)
    {
        switch (node)
        {
            case JsonObject obj:
                WriteObject(text, obj, depth, ListLineStarts.None);
                break;
            case JsonArray array when !HasContainers(array):
                WriteFilledList(text, array, depth, _ => false);
                break;
            case JsonArray array:
                WriteList(text, array, depth);
                break;
            default:
                text.Append(Inline(node));
                break;
        }
    }

    private static void WriteObject(StringBuilder text, JsonObject obj, int depth, ListLineStarts layout)
    {
        if (obj.Count == 0)
        {
            text.Append("{}");
            return;
        }

        text.Append('{');
        int index = 0;
        foreach (KeyValuePair<string, JsonNode?> field in obj)
        {
            int lineStart = StartLine(text, depth + 1);
            text.Append(Value(JsonValue.Create(field.Key))).Append(": ");
            if (field.Value is JsonArray list && !HasContainers(list) && layout.IsWrapped(field.Key, list))
            {
                string key = field.Key;
                WriteFilledList(text, list, depth + 1, item => layout.StartsLine(key, item));
            }
            else
            {
                WriteNode(text, field.Value, depth + 1, lineStart, isListItem: false);
            }

            text.Append(++index < obj.Count ? "," : "");
        }

        StartLine(text, depth);
        text.Append('}');
    }

    private static void WriteList(StringBuilder text, JsonArray array, int depth)
    {
        text.Append('[');
        for (int i = 0; i < array.Count; i++)
        {
            int lineStart = StartLine(text, depth + 1);
            WriteNode(text, array[i], depth + 1, lineStart, isListItem: true);
            text.Append(i + 1 < array.Count ? "," : "");
        }

        StartLine(text, depth);
        text.Append(']');
    }

    // Long lists of names (a character's move list) fill each line instead of taking one line per name, and start a
    // new line where the file was wrapped by hand.
    private static void WriteFilledList(
        StringBuilder text, JsonArray array, int depth, Func<JsonNode?, bool> startsLine)
    {
        text.Append('[');
        int lineStart = StartLine(text, depth + 1);
        for (int i = 0; i < array.Count; i++)
        {
            string item = Inline(array[i]) + (i + 1 < array.Count ? "," : "");
            bool lineIsEmpty = text.Length - lineStart == (depth + 1) * Indent.Length;
            if (!lineIsEmpty && (startsLine(array[i]) || text.Length - lineStart + 1 + item.Length > LineWidth))
            {
                lineStart = StartLine(text, depth + 1);
                lineIsEmpty = true;
            }

            text.Append(lineIsEmpty ? "" : " ").Append(item);
        }

        StartLine(text, depth);
        text.Append(']');
    }

    private static int StartLine(StringBuilder text, int depth)
    {
        text.Append('\n');
        int lineStart = text.Length;
        for (int i = 0; i < depth; i++)
        {
            text.Append(Indent);
        }

        return lineStart;
    }

    private static string Inline(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj.Count == 0)
                {
                    return "{}";
                }

                List<string> fields = [];
                foreach (KeyValuePair<string, JsonNode?> field in obj)
                {
                    fields.Add($"{Value(JsonValue.Create(field.Key))}: {Inline(field.Value)}");
                }

                return "{ " + string.Join(", ", fields) + " }";
            case JsonArray array:
                List<string> items = [];
                foreach (JsonNode? item in array)
                {
                    items.Add(Inline(item));
                }

                return "[" + string.Join(", ", items) + "]";
            default:
                return Value(node);
        }
    }

    private static string Value(JsonNode? node) => node is null ? "null" : node.ToJsonString(_valueOptions);

    private static bool HasContainers(JsonArray array)
    {
        foreach (JsonNode? item in array)
        {
            if (item is JsonObject or JsonArray)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasOnlyValues(JsonObject obj)
    {
        foreach (KeyValuePair<string, JsonNode?> field in obj)
        {
            if (field.Value is JsonObject or JsonArray)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasObjects(JsonObject obj)
    {
        foreach (KeyValuePair<string, JsonNode?> field in obj)
        {
            if (field.Value is JsonObject || (field.Value is JsonArray array && HasContainers(array)))
            {
                return true;
            }
        }

        return false;
    }
}
