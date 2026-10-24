using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FightingGame.Input.Profiles;

/// <summary>
/// A list of <see cref="ControlPreset"/>s as JSON, each <c>{ "id", "name", "kind", "profile" }</c>: the built-in
/// presets file and the presets kept in the controls file share it.
/// </summary>
public static class ControlPresetJson
{
    private const string KeyboardKind = "keyboard";
    private const string JoypadKind = "joypad";

    public static JsonArray Write(IReadOnlyList<ControlPreset> presets)
    {
        var list = new JsonArray();
        foreach (ControlPreset preset in presets)
        {
            list.Add(new JsonObject
            {
                ["id"] = preset.Id,
                ["name"] = preset.NameKey,
                ["kind"] = preset.Profile.Kind == InputDeviceKind.Keyboard ? KeyboardKind : JoypadKind,
                ["profile"] = ProfileJson.Write(preset.Profile),
            });
        }

        return list;
    }

    /// <param name="defaultOf">The layout missing parts fall back to, per kind; null reads against an empty one.</param>
    public static List<ControlPreset> Read(
        JsonNode? node,
        BindingTargets targets,
        System.Func<InputDeviceKind, BindingProfile>? defaultOf,
        string path,
        List<string> warnings)
    {
        var presets = new List<ControlPreset>();
        if (node is not JsonArray list)
        {
            warnings.Add($"{path}: not a list, no presets read");
            return presets;
        }

        for (int index = 0; index < list.Count; index++)
        {
            string presetPath = $"{path}[{index}]";
            if (list[index] is not JsonObject preset ||
                Text(preset, "id") is not { } id || Text(preset, "name") is not { } name ||
                KindOf(Text(preset, "kind")) is not { } kind)
            {
                warnings.Add($"{presetPath}: needs an id, a name and a kind (keyboard or joypad), dropped");
                continue;
            }

            BindingProfile fallback = defaultOf?.Invoke(kind) ?? Empty(kind);
            BindingProfile profile = ProfileJson.Read(preset["profile"], kind, targets, fallback, $"{presetPath}.profile", warnings);
            presets.Add(new ControlPreset(id, name, profile));
        }

        return presets;
    }

    public static BindingProfile Empty(InputDeviceKind kind) => new(kind, [], [], StickOptions.None);

    private static string? Text(JsonObject json, string name) =>
        json[name] is JsonValue value && value.TryGetValue(out string? text) && text.Length > 0 ? text : null;

    private static InputDeviceKind? KindOf(string? name) => name switch
    {
        KeyboardKind => InputDeviceKind.Keyboard,
        JoypadKind => InputDeviceKind.Joypad,
        _ => null,
    };
}
