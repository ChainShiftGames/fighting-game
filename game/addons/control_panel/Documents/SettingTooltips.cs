using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>
/// The tooltip for a setting: the panel's own one-line hint, then the field's <c>description</c> from the JSON
/// schema its data file names (<c>data/schema/</c>), so the schema stays the one place a data field is documented.
/// </summary>
/// <remarks>Schemas are read once and kept; Reload in the panel makes a new instance.</remarks>
public sealed class SettingTooltips
{
    private const string SchemaKey = "$schema";

    private readonly PanelDocument _document;
    private readonly Dictionary<string, JsonElement?> _schemas = new(StringComparer.OrdinalIgnoreCase);

    public SettingTooltips(PanelDocument document)
    {
        _document = document;
    }

    public string For(PanelSetting setting)
    {
        string description = SchemaDescription(setting);
        if (description.Length == 0 || description == setting.Hint)
        {
            return setting.Hint;
        }

        return setting.Hint.Length == 0 ? description : $"{setting.Hint}\n\n{description}";
    }

    /// <returns>The schema's description of the field; empty when the file names no schema or it says nothing.</returns>
    public string SchemaDescription(PanelSetting setting)
    {
        try
        {
            return Schema(setting.File) is JsonElement schema ? Describe(schema, setting.Path) : string.Empty;
        }
        catch (Exception exception) when (exception is IOException or FormatException or JsonException)
        {
            return string.Empty;
        }
    }

    private JsonElement? Schema(PanelFile file)
    {
        if (!_document.Exists(file) || !JsonPathField.Has(_document.Text(file), SchemaKey))
        {
            return null;
        }

        string reference = JsonPathField.ReadString(_document.Text(file), SchemaKey);
        string folder = Path.GetDirectoryName(_document.Folders.FullPath(file))!;
        string path = Path.GetFullPath(Path.Combine(folder, reference.Replace('/', Path.DirectorySeparatorChar)));
        if (!_schemas.TryGetValue(path, out JsonElement? schema))
        {
            schema = File.Exists(path) ? JsonDocument.Parse(File.ReadAllText(path)).RootElement : null;
            _schemas[path] = schema;
        }

        return schema;
    }

    // Walks "properties" for keys and "items" for list entries, following local "$ref"s; a description next to a
    // $ref wins over the referenced definition's own.
    private static string Describe(JsonElement root, string path)
    {
        JsonElement node = root;
        foreach (string segment in JsonPathField.Segments(path))
        {
            node = Resolve(root, node);
            string container = segment.StartsWith('[') ? "items" : "properties";
            if (!node.TryGetProperty(container, out JsonElement child))
            {
                return string.Empty;
            }

            if (container == "properties" && !child.TryGetProperty(segment, out child))
            {
                return string.Empty;
            }

            node = child;
        }

        return Description(node) is { Length: > 0 } own ? own : Description(Resolve(root, node));
    }

    private static JsonElement Resolve(JsonElement root, JsonElement node)
    {
        while (node.ValueKind == JsonValueKind.Object && node.TryGetProperty("$ref", out JsonElement reference)
            && reference.GetString() is string target && target.StartsWith("#/", StringComparison.Ordinal))
        {
            JsonElement found = root;
            foreach (string step in target[2..].Split('/'))
            {
                if (!found.TryGetProperty(step, out found))
                {
                    return node;
                }
            }

            node = found;
        }

        return node;
    }

    private static string Description(JsonElement node) =>
        node.ValueKind == JsonValueKind.Object && node.TryGetProperty("description", out JsonElement text)
            ? text.GetString() ?? string.Empty
            : string.Empty;
}
