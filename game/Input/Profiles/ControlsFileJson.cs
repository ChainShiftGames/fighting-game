using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FightingGame.Input.Profiles;

/// <summary>
/// The controls file's JSON: <c>schemaVersion</c>, the <c>keyboard</c> layout, <c>joypadDefault</c>, each joypad's
/// own layout by <c>guid</c> + <c>ordinal</c> (ADR-0038 §2) and kept <c>presets</c>.
/// </summary>
/// <remarks>
/// Lenient like the other player settings: a bad part falls back with a warning. A newer version is read by the
/// names this game knows. A later format change upgrades the older JSON here and bumps <see cref="SchemaVersion"/>.
/// </remarks>
public static class ControlsFileJson
{
    /// <summary>1: per-device layouts (UI-10).</summary>
    public const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public static string Write(ControlsFile file)
    {
        var json = new JsonObject { ["schemaVersion"] = SchemaVersion };
        if (file.Keyboard is { } keyboard)
        {
            json["keyboard"] = ProfileJson.Write(keyboard);
        }

        if (file.JoypadDefault is { } joypadDefault)
        {
            json["joypadDefault"] = ProfileJson.Write(joypadDefault);
        }

        var joypads = new JsonArray();
        foreach (StoredJoypadProfile joypad in file.Joypads)
        {
            joypads.Add(new JsonObject
            {
                ["guid"] = joypad.Device.Guid,
                ["ordinal"] = joypad.Device.Ordinal,
                ["name"] = joypad.Name,
                ["profile"] = ProfileJson.Write(joypad.Profile),
            });
        }

        json["joypads"] = joypads;
        json["presets"] = ControlPresetJson.Write(file.Presets);
        return json.ToJsonString(_json);
    }

    /// <param name="warning">What was replaced by defaults; empty when nothing was.</param>
    /// <param name="isUnreadable">The whole file was unreadable: nothing in it was used.</param>
    public static ControlsFile Read(
        string text, BindingTargets targets, BuiltInPresets builtIn, out string warning, out bool isUnreadable)
    {
        var warnings = new List<string>();
        JsonObject? json;
        try
        {
            json = JsonNode.Parse(text) as JsonObject;
        }
        catch (JsonException exception)
        {
            warnings.Add($"not valid JSON: {exception.Message}");
            json = null;
        }

        isUnreadable = json is null;
        if (json is null)
        {
            warning = warnings.Count > 0 ? warnings[0] : "not a JSON object";
            return ControlsFile.Empty;
        }

        CheckVersion(json, warnings);
        BindingProfile? keyboard = ReadOptional(json, "keyboard", InputDeviceKind.Keyboard, targets, builtIn, warnings);
        BindingProfile? joypadDefault = ReadOptional(json, "joypadDefault", InputDeviceKind.Joypad, targets, builtIn, warnings);
        List<StoredJoypadProfile> joypads = ReadJoypads(json["joypads"], targets, builtIn, warnings);
        List<ControlPreset> presets = json["presets"] is null
            ? []
            : ControlPresetJson.Read(json["presets"], targets, builtIn.DefaultOf, "presets", warnings);
        warning = string.Join("; ", warnings);
        return new ControlsFile(keyboard, joypadDefault, joypads, presets);
    }

    private static void CheckVersion(JsonObject json, List<string> warnings)
    {
        if (json["schemaVersion"] is not JsonValue value || !value.TryGetValue(out int version))
        {
            warnings.Add($"schemaVersion is missing, reading it as {SchemaVersion}");
        }
        else if (version > SchemaVersion)
        {
            warnings.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"schemaVersion {version} is newer than {SchemaVersion}; reading the layouts this game knows"));
        }
    }

    private static BindingProfile? ReadOptional(
        JsonObject json,
        string name,
        InputDeviceKind kind,
        BindingTargets targets,
        BuiltInPresets builtIn,
        List<string> warnings) =>
        json[name] is { } node ? ProfileJson.Read(node, kind, targets, builtIn.DefaultOf(kind), name, warnings) : null;

    private static List<StoredJoypadProfile> ReadJoypads(
        JsonNode? node, BindingTargets targets, BuiltInPresets builtIn, List<string> warnings)
    {
        var joypads = new List<StoredJoypadProfile>();
        if (node is not JsonArray list)
        {
            return joypads;
        }

        for (int index = 0; index < list.Count; index++)
        {
            string path = $"joypads[{index}]";
            if (list[index] is not JsonObject joypad ||
                joypad["guid"] is not JsonValue guidValue || !guidValue.TryGetValue(out string? guid) ||
                joypad["ordinal"] is not JsonValue ordinalValue || !ordinalValue.TryGetValue(out int ordinal) || ordinal < 0)
            {
                warnings.Add($"{path}: needs a guid and an ordinal, dropped");
                continue;
            }

            string name = joypad["name"] is JsonValue nameValue && nameValue.TryGetValue(out string? text) ? text : guid;
            BindingProfile profile = ProfileJson.Read(
                joypad["profile"], InputDeviceKind.Joypad, targets, builtIn.DefaultOf(InputDeviceKind.Joypad), $"{path}.profile", warnings);
            joypads.Add(new StoredJoypadProfile(InputDeviceId.Joypad(guid, ordinal), name, profile));
        }

        return joypads;
    }
}
