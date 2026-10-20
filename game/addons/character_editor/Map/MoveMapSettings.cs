using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>
/// How the move map names and groups moves (developer setting, editor only; <c>move_map.json</c> next to the
/// plugin): which buttons share a column of normals, plain names for buttons, stances and special motions, and which
/// of the character's states count as movement rather than reactions.
/// </summary>
/// <param name="NormalColumns">Columns of normals, left to right; a normal goes in the first naming its button.</param>
/// <param name="ButtonNames">A button's strength, e.g. <c>b</c> = Medium; a button without one shows as its id.</param>
/// <param name="StanceNames">A normal's stance by motion, e.g. <c>2</c> = Crouching; <c>air</c> for air normals.</param>
/// <param name="SpecialNames">A special move's name by motion, e.g. <c>236</c> = Quarter-circle.</param>
/// <param name="MovementStates">Keys of <c>character.json</c> states whose moves are movement.</param>
public sealed record MoveMapSettings(
    IReadOnlyList<MapColumnSetting> NormalColumns,
    IReadOnlyDictionary<string, string> ButtonNames,
    IReadOnlyDictionary<string, string> StanceNames,
    IReadOnlyDictionary<string, string> SpecialNames,
    IReadOnlyList<string> MovementStates)
{
    /// <summary>The <see cref="StanceNames"/> key for moves done in the air.</summary>
    public const string AirStance = "air";

    /// <summary>No columns or names: every normal lands in one column and moves show their ids.</summary>
    public static MoveMapSettings Empty { get; } = new(
        [], new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>(), []);

    /// <summary>Reads the settings file; missing or malformed parts are left empty rather than failing the editor.</summary>
    public static MoveMapSettings Parse(string json)
    {
        JsonObject? root;
        try
        {
            root = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return Empty;
        }

        if (root is null)
        {
            return Empty;
        }

        List<MapColumnSetting> columns = [];
        foreach (JsonNode? column in root["normalColumns"] as JsonArray ?? [])
        {
            if (column is JsonObject obj)
            {
                columns.Add(new MapColumnSetting(
                    JsonNodeValue.Text(obj["title"]) ?? "", JsonNodeValue.Text(obj["hint"]) ?? "",
                    JsonStringList.Read(obj, "buttons")));
            }
        }

        return new MoveMapSettings(
            columns, Names(root["buttonNames"]), Names(root["stanceNames"]), Names(root["specialNames"]),
            JsonStringList.Read(root, "movementStates"));
    }

    private static Dictionary<string, string> Names(JsonNode? node)
    {
        Dictionary<string, string> names = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, JsonNode?> entry in node as JsonObject ?? [])
        {
            if (JsonNodeValue.Text(entry.Value) is string name)
            {
                names[entry.Key] = name;
            }
        }

        return names;
    }
}
