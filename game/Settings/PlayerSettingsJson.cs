using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FightingGame.Settings;

/// <summary>
/// Reads and writes the <see cref="PlayerSettings"/> file's JSON. A value out of range falls back to its default and an
/// unreadable file to all the defaults, with a warning: a bad settings file must never stop the game.
/// </summary>
/// <remarks>
/// The file carries <see cref="SchemaVersion"/>. Version 1 is the file from before it had one (vsync and input delay):
/// its names are unchanged, so it reads as it is and gets the new values' defaults. A newer version is read too, by
/// the names this game knows. A later rename or meaning change upgrades the older JSON in <see cref="Read"/>.
/// </remarks>
public static class PlayerSettingsJson
{
    /// <summary>2: video, audio and language settings, and the version itself.</summary>
    public const int SchemaVersion = 2;

    /// <summary>What a file without a version is.</summary>
    public const int UnversionedSchema = 1;

    private const string VersionProperty = "schemaVersion";

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new LenientEnumConverter<ScreenMode>() },
    };

    public static string Write(PlayerSettings settings)
    {
        JsonObject values = JsonSerializer.SerializeToNode(settings, _json)!.AsObject();
        var file = new JsonObject { [VersionProperty] = SchemaVersion };
        foreach (KeyValuePair<string, JsonNode?> value in new List<KeyValuePair<string, JsonNode?>>(values))
        {
            values.Remove(value.Key);
            file[value.Key] = value.Value;
        }

        return file.ToJsonString(_json);
    }

    /// <param name="warning">Why values or the whole file were replaced by defaults; empty when nothing was.</param>
    public static PlayerSettings Read(string json, out string warning)
    {
        PlayerSettings settings;
        var warnings = new List<string>();
        try
        {
            if (JsonNode.Parse(json) is not JsonObject file)
            {
                warning = "not a JSON object, using the defaults";
                return new PlayerSettings();
            }

            int version = VersionOf(file, warnings);
            if (version > SchemaVersion)
            {
                warnings.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"schemaVersion {version} is newer than {SchemaVersion}; reading the settings this game knows"));
            }

            file.Remove(VersionProperty);
            settings = file.Deserialize<PlayerSettings>(_json) ?? new PlayerSettings();
        }
        catch (JsonException exception)
        {
            warning = $"unreadable, using the defaults: {exception.Message}";
            return new PlayerSettings();
        }
        catch (System.InvalidOperationException exception)
        {
            warning = $"unreadable, using the defaults: {exception.Message}";
            return new PlayerSettings();
        }

        settings = PlayerSettingsCheck.Fix(settings, warnings);
        warning = string.Join("; ", warnings);
        return settings;
    }

    private static int VersionOf(JsonObject file, List<string> warnings)
    {
        if (!file.TryGetPropertyValue(VersionProperty, out JsonNode? node))
        {
            return UnversionedSchema;
        }

        if (node is JsonValue value && value.TryGetValue(out int version) && version >= UnversionedSchema)
        {
            return version;
        }

        warnings.Add($"schemaVersion {node?.ToJsonString()} is not a version, reading it as {SchemaVersion}");
        return SchemaVersion;
    }
}
