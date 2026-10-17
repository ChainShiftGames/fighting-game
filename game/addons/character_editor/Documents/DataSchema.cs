using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// The fields a data file kind has, read from its JSON Schema in <c>data/schema/</c>. The editor builds its generic
/// forms from this, so a field added to the schema (e.g. a module's per-character data) becomes editable without
/// editor changes. Only the shape is read; Sim.Data's validation stays the authority on what is allowed.
/// </summary>
public sealed class DataSchema
{
    // Deeper than any data file nests; stops a schema whose $refs loop from recursing forever.
    private const int MaxDepth = 8;
    private const string LocalDefinitionPrefix = "#/$defs/";

    private readonly JsonObject _schema;

    private DataSchema(JsonObject schema)
    {
        _schema = schema;
        Root = ReadObject(schema, 0);
    }

    /// <summary>The fields of the file's root object.</summary>
    public SchemaObject Root { get; }

    /// <summary>Data-relative path of a file kind's schema, e.g. <c>schema/move.schema.json</c>.</summary>
    public static string PathOf(string kind) => $"schema/{kind}.schema.json";

    /// <summary>Reads the schema of a file kind; null when there is none or it can't be read.</summary>
    public static DataSchema? Read(IDataSource source, string kind)
    {
        string path = PathOf(kind);
        if (!source.FileExists(path))
        {
            return null;
        }

        JsonObject? schema = JsonDataReader.Read(source, path, new DataErrors());
        return schema is null ? null : new DataSchema(schema);
    }

    /// <summary>A schema from its text, for tests and tools.</summary>
    /// <exception cref="JsonException">The text isn't a JSON object.</exception>
    public static DataSchema Parse(string json) =>
        new(JsonNode.Parse(json) as JsonObject ?? throw new JsonException("A schema is a JSON object."));

    private SchemaObject ReadObject(JsonObject node, int depth)
    {
        if (depth > MaxDepth || node["properties"] is not JsonObject properties)
        {
            return SchemaObject.Empty;
        }

        HashSet<string> required = new(StringComparer.Ordinal);
        foreach (JsonNode? name in node["required"] as JsonArray ?? [])
        {
            if (JsonNodeValue.Text(name) is string text)
            {
                required.Add(text);
            }
        }

        List<SchemaField> fields = [];
        foreach (KeyValuePair<string, JsonNode?> property in properties)
        {
            if (property.Value is JsonObject definition)
            {
                fields.Add(ReadField(property.Key, definition, required.Contains(property.Key), depth));
            }
        }

        return new SchemaObject(fields);
    }

    private SchemaField ReadField(string name, JsonObject definition, bool isRequired, int depth)
    {
        JsonObject resolved = Resolve(definition);
        string description = JsonNodeValue.Text(definition["description"])
            ?? JsonNodeValue.Text(resolved["description"]) ?? "";
        SchemaField field = new(name, SchemaFieldKind.Other, isRequired, description, null, null, [], null, null);
        if (Choices(resolved) is { Count: > 0 } choices)
        {
            return field with { Kind = SchemaFieldKind.Choice, Choices = choices };
        }

        switch (JsonNodeValue.Text(resolved["type"]))
        {
            case "integer":
                return field with
                {
                    Kind = SchemaFieldKind.Integer,
                    Minimum = Whole(resolved["minimum"]),
                    Maximum = Whole(resolved["maximum"]),
                };
            case "string" when resolved["const"] is null:
                return field with { Kind = SchemaFieldKind.Text };
            case "boolean":
                return field with { Kind = SchemaFieldKind.Boolean };
            case "array":
                JsonObject items = resolved["items"] is JsonObject itemNode ? Resolve(itemNode) : new JsonObject();
                return Choices(items) is { Count: > 0 } itemChoices
                    ? field with { Kind = SchemaFieldKind.ChoiceList, Choices = itemChoices }
                    : field with { Items = items["properties"] is null ? null : ReadObject(items, depth + 1) };
            case "object":
            case null when resolved["properties"] is not null:
                return field with { Kind = SchemaFieldKind.Object, Object = ReadObject(resolved, depth + 1) };
            default:
                return field;
        }
    }

    // Only local references ("#/$defs/frames"), the only kind data/schema uses.
    private JsonObject Resolve(JsonObject definition)
    {
        string? reference = JsonNodeValue.Text(definition["$ref"]);
        if (reference is null || !reference.StartsWith(LocalDefinitionPrefix, StringComparison.Ordinal))
        {
            return definition;
        }

        return _schema["$defs"]?[reference[LocalDefinitionPrefix.Length..]] as JsonObject ?? definition;
    }

    private static List<string>? Choices(JsonObject definition)
    {
        if (definition["enum"] is not JsonArray values)
        {
            return null;
        }

        List<string> choices = [];
        foreach (JsonNode? value in values)
        {
            if (JsonNodeValue.Text(value) is string text)
            {
                choices.Add(text);
            }
        }

        return choices;
    }

    private static long? Whole(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue(out long number) ? number : null;
}
