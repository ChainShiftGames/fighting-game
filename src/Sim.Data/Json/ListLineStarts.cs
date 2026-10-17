using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sim.Data.Json;

/// <summary>
/// Which names in a file's top-level lists of names started a line when the file was read, so saving keeps lists
/// that were wrapped by hand (e.g. a move list grouped into attacks and basic states) instead of refilling them.
/// </summary>
/// <remarks>
/// A line start belongs to the name, not to its position: a group still starts with the same move after reordering.
/// Names added later fill lines as usual.
/// </remarks>
public sealed class ListLineStarts
{
    private readonly Dictionary<string, HashSet<string>> _starts;

    private ListLineStarts(Dictionary<string, HashSet<string>> starts)
    {
        _starts = starts;
    }

    /// <summary>No hand wrapping: lists are filled to the line width.</summary>
    public static ListLineStarts None { get; } = new(new Dictionary<string, HashSet<string>>(StringComparer.Ordinal));

    /// <summary>Reads the layout of a file's text (UTF-8, no BOM); <see cref="None"/> when it isn't valid JSON.</summary>
    public static ListLineStarts Read(ReadOnlySpan<byte> json)
    {
        Dictionary<string, HashSet<string>> starts = new(StringComparer.Ordinal);
        try
        {
            Collect(json, starts);
        }
        catch (JsonException)
        {
            return None;
        }

        return new ListLineStarts(starts);
    }

    /// <summary>The top-level list <paramref name="field"/> was wrapped by hand after its first name.</summary>
    public bool IsWrapped(string field, JsonArray list)
    {
        for (int i = 1; i < list.Count; i++)
        {
            if (StartsLine(field, list[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The name <paramref name="item"/> of the top-level list <paramref name="field"/> started a line.</summary>
    public bool StartsLine(string field, JsonNode? item) =>
        item?.GetValueKind() == JsonValueKind.String && _starts.TryGetValue(field, out HashSet<string>? names)
        && names.Contains(item.GetValue<string>());

    private static void Collect(ReadOnlySpan<byte> json, Dictionary<string, HashSet<string>> starts)
    {
        Utf8JsonReader reader = new(json);
        string? field = null;
        HashSet<string>? names = null;
        while (reader.Read())
        {
            // Depth 1: the root object's fields; depth 2: the items of a top-level list.
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName when reader.CurrentDepth == 1:
                    field = reader.GetString();
                    break;
                case JsonTokenType.StartArray when reader.CurrentDepth == 1:
                    names = new HashSet<string>(StringComparer.Ordinal);
                    break;
                case JsonTokenType.EndArray when reader.CurrentDepth == 1:
                    if (field is not null && names is { Count: > 0 })
                    {
                        starts[field] = names;
                    }

                    names = null;
                    break;
                case JsonTokenType.String when reader.CurrentDepth == 2 && names is not null:
                    if (IsFirstOnLine(json, (int)reader.TokenStartIndex))
                    {
                        names.Add(reader.GetString()!);
                    }

                    break;
            }
        }
    }

    private static bool IsFirstOnLine(ReadOnlySpan<byte> json, int tokenStart)
    {
        for (int i = tokenStart - 1; i >= 0; i--)
        {
            switch (json[i])
            {
                case (byte)'\n':
                    return true;
                case (byte)' ' or (byte)'\t' or (byte)'\r':
                    continue;
                default:
                    return false;
            }
        }

        return true;
    }
}
