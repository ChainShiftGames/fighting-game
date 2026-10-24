using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using FightingGame.Presentation.World;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Rewrites one move's contact frames and timing keys in an art file's text (ADR-0028). Only that move's entry under
/// <c>animations</c> changes, so the file keeps its comments, layout and the other entries exactly, and the git diff
/// shows just the timing. A move without an entry gets one at the end of <c>animations</c>.
/// </summary>
public static class ArtTimingText
{
    private const string AnimationsKey = "animations";
    private const string ActiveFromKey = "activeFrom";
    private const string RecoveryFromKey = "recoveryFrom";
    private const string KeysKey = "keys";
    private const string Indent = "  ";

    private static readonly JsonReaderOptions _reader = new() { CommentHandling = JsonCommentHandling.Skip };
    private static readonly JsonDocumentOptions _document = new() { CommentHandling = JsonCommentHandling.Skip };

    // Keeps "°", "→" and "'" as written instead of \u escapes.
    private static readonly JsonSerializerOptions _values = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <exception cref="FormatException">The text isn't JSON with an <c>animations</c> object.</exception>
    public static string Write(string json, string moveId, MoveAnimationTiming timing)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        string newline = json.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        (int Start, int End) animations = Find(bytes, moveId, out (int Start, int End)? entry, out int lastEnd);
        if (entry is { } found)
        {
            JsonObject old = JsonNode.Parse(bytes.AsSpan(found.Start, found.End - found.Start), null, _document)!.AsObject();
            return JsonTextEdits.Splice(bytes, found.Start, found.End, Entry(old, timing, JsonTextEdits.IndentOfLine(bytes, found.Start), newline));
        }

        if (lastEnd >= 0)
        {
            string indent = JsonTextEdits.IndentOfLine(bytes, lastEnd);
            return JsonTextEdits.Splice(bytes, lastEnd, lastEnd, $",{newline}{indent}{Added(moveId, timing, indent, newline)}");
        }

        // An empty object: "{}" gets the entry on its own line; anything between the braces (a comment) stays.
        string outer = JsonTextEdits.IndentOfLine(bytes, animations.Start);
        string inner = outer + Indent;
        string added = Added(moveId, timing, inner, newline);
        bool empty = Encoding.UTF8.GetString(bytes, animations.Start + 1, animations.End - animations.Start - 1).Trim().Length == 0;
        return empty
            ? JsonTextEdits.Splice(bytes, animations.Start + 1, animations.End, $"{newline}{inner}{added}{newline}{outer}")
            : JsonTextEdits.Splice(bytes, animations.Start + 1, animations.Start + 1, $"{newline}{inner}{added},");
    }

    // Where `animations` is (its braces), the move's entry in it, and where its last entry ends (-1 for none).
    private static (int Start, int End) Find(byte[] bytes, string moveId, out (int Start, int End)? entry, out int lastEnd)
    {
        entry = null;
        lastEnd = -1;
        try
        {
            var reader = new Utf8JsonReader(bytes, _reader);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                bool isAnimations = reader.ValueTextEquals(AnimationsKey);
                reader.Read();
                if (isAnimations && reader.TokenType == JsonTokenType.StartObject)
                {
                    int start = (int)reader.TokenStartIndex;
                    while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                    {
                        bool isMove = reader.ValueTextEquals(moveId);
                        reader.Read();
                        int valueStart = (int)reader.TokenStartIndex;
                        bool isObject = reader.TokenType == JsonTokenType.StartObject;
                        reader.Skip();
                        lastEnd = (int)reader.BytesConsumed;
                        if (isMove && isObject)
                        {
                            entry = (valueStart, lastEnd);
                        }
                    }

                    return (start, (int)reader.TokenStartIndex);
                }

                reader.Skip();
            }
        }
        catch (JsonException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        throw new FormatException($"the art file has no '{AnimationsKey}' object");
    }

    private static string Added(string moveId, MoveAnimationTiming timing, string indent, string newline) =>
        $"\"{moveId}\": {Entry([], timing, indent, newline)}";

    // The entry's other fields as they were, in their order, then the contact frames and keys.
    private static string Entry(JsonObject old, MoveAnimationTiming timing, string indent, string newline)
    {
        var fields = new List<string>();
        foreach (KeyValuePair<string, JsonNode?> field in old)
        {
            if (field.Key is not (ActiveFromKey or RecoveryFromKey or KeysKey))
            {
                fields.Add($"\"{field.Key}\": {field.Value?.ToJsonString(_values) ?? "null"}");
            }
        }

        if (timing.Contacts is { } contacts)
        {
            fields.Add($"\"{ActiveFromKey}\": {contacts.ActiveFrom}");
            fields.Add($"\"{RecoveryFromKey}\": {contacts.RecoveryFrom}");
        }

        if (timing.Keys.Count == 0)
        {
            return fields.Count == 0 ? "{}" : $"{{ {string.Join(", ", fields)} }}";
        }

        string inner = indent + Indent;
        var text = new StringBuilder("{").Append(newline);
        if (fields.Count > 0)
        {
            text.Append(inner).Append(string.Join(", ", fields)).Append(',').Append(newline);
        }

        text.Append(inner).Append($"\"{KeysKey}\": [").Append(newline);
        for (int i = 0; i < timing.Keys.Count; i++)
        {
            TimingKey key = timing.Keys[i];
            string phase = JsonNamingPolicy.CamelCase.ConvertName(key.Phase.ToString());
            text.Append(inner).Append(Indent)
                .Append($"{{ \"phase\": \"{phase}\", \"frame\": {key.Frame}, \"of\": {key.Of}, \"clip\": {key.Clip} }}")
                .Append(i < timing.Keys.Count - 1 ? "," : "").Append(newline);
        }

        return text.Append(inner).Append(']').Append(newline).Append(indent).Append('}').ToString();
    }
}
