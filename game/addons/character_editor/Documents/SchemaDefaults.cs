using System;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// The value the editor puts in a field the user adds. Taken from the schema alone (0 or the nearest allowed whole
/// number, the first choice, empty text), so the editor has no gameplay defaults of its own; the user then sets the
/// real value.
/// </summary>
public static class SchemaDefaults
{
    public static JsonNode? ValueFor(SchemaField field) => field.Kind switch
    {
        SchemaFieldKind.Integer => JsonValue.Create(
            (int)Math.Clamp(0L, field.Minimum ?? 0L, Math.Min(field.Maximum ?? int.MaxValue, int.MaxValue))),
        SchemaFieldKind.Text => JsonValue.Create(""),
        SchemaFieldKind.Boolean => JsonValue.Create(false),
        SchemaFieldKind.Choice => JsonValue.Create(field.Choices.Count > 0 ? field.Choices[0] : ""),
        SchemaFieldKind.Object => ObjectFor(field.Object ?? SchemaObject.Empty),
        _ => new JsonArray(),
    };

    /// <summary>An object with the schema's required fields only, each at its default.</summary>
    public static JsonObject ObjectFor(SchemaObject schema)
    {
        JsonObject obj = new();
        foreach (SchemaField field in schema.Fields)
        {
            if (field.IsRequired)
            {
                obj.Add(field.Name, ValueFor(field));
            }
        }

        return obj;
    }
}
