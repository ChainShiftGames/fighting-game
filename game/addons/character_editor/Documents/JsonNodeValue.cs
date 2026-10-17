using System.Text.Json;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Reads a plain value from a hand-edited JSON tree without throwing: a field may hold anything until Sim.Data's
/// validation reports it, and the editor still has to show the file.
/// </summary>
public static class JsonNodeValue
{
    /// <summary>The node's whole number; null when it is missing or not a whole number in range.</summary>
    public static int? Int(JsonNode? node)
    {
        if (node is not JsonValue value || value.GetValueKind() != JsonValueKind.Number)
        {
            return null;
        }

        // A value the editor created holds a long or an int; one read from a file converts to either.
        if (value.TryGetValue(out int number))
        {
            return number;
        }

        return value.TryGetValue(out long wide) && wide is >= int.MinValue and <= int.MaxValue ? (int)wide : null;
    }

    /// <summary>The node's string; null when it is missing or not a string.</summary>
    public static string? Text(JsonNode? node) =>
        node?.GetValueKind() == JsonValueKind.String ? node.GetValue<string>() : null;

    /// <summary>The node's true/false; null when it is missing or not a boolean.</summary>
    public static bool? Flag(JsonNode? node) => node?.GetValueKind() switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null,
    };
}
