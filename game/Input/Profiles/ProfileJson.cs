using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FightingGame.Input.Profiles;

/// <summary>
/// One <see cref="BindingProfile"/> as JSON: <c>bindings</c> and <c>menu</c> map each input to a list of control
/// names, and a joypad adds <c>leftStick</c>, <c>stickDeadzone</c> and <c>triggerThreshold</c>.
/// </summary>
/// <remarks>
/// Reading is lenient, like the other player settings: an unknown input or control is dropped and a missing or
/// mistyped part comes from the fallback, each with a warning that says where. A bad file never stops the game.
/// </remarks>
public static class ProfileJson
{
    private const string BindingsField = "bindings";
    private const string MenuField = "menu";
    private const string LeftStickField = "leftStick";
    private const string DeadzoneField = "stickDeadzone";
    private const string ThresholdField = "triggerThreshold";

    public static JsonObject Write(BindingProfile profile)
    {
        var json = new JsonObject();
        if (profile.Kind == InputDeviceKind.Joypad)
        {
            json[LeftStickField] = profile.Stick.LeftStick;
            json[DeadzoneField] = Math.Round(profile.Stick.StickDeadzone, 2);
            json[ThresholdField] = Math.Round(profile.Stick.TriggerThreshold, 2);
        }

        json[BindingsField] = WriteLayer(profile.Game);
        json[MenuField] = WriteLayer(profile.Menu);
        return json;
    }

    /// <param name="fallback">Gives every part that is missing or unreadable.</param>
    /// <param name="path">Where <paramref name="node"/> is in the file, for the warnings.</param>
    public static BindingProfile Read(
        JsonNode? node,
        InputDeviceKind kind,
        BindingTargets targets,
        BindingProfile fallback,
        string path,
        List<string> warnings)
    {
        if (node is not JsonObject json)
        {
            warnings.Add($"{path}: not an object, using the default layout");
            return fallback;
        }

        IReadOnlyList<ProfileBinding> game = fallback.Game;
        IReadOnlyList<ProfileBinding> menu = fallback.Menu;
        StickOptions stick = fallback.Stick;
        foreach ((string name, JsonNode? value) in json)
        {
            string field = $"{path}.{name}";
            switch (name)
            {
                case BindingsField:
                    game = ReadLayer(value, kind, isMenu: false, field, warnings, input => IsGameInput(targets, input), game);
                    break;
                case MenuField:
                    menu = ReadLayer(value, kind, isMenu: true, field, warnings, IsMenuInput, menu);
                    break;
                case LeftStickField when kind == InputDeviceKind.Joypad:
                    stick = stick with { LeftStick = ReadBool(value, field, warnings) ?? stick.LeftStick };
                    break;
                case DeadzoneField when kind == InputDeviceKind.Joypad:
                    stick = stick with { StickDeadzone = ReadFraction(value, field, warnings) ?? stick.StickDeadzone };
                    break;
                case ThresholdField when kind == InputDeviceKind.Joypad:
                    stick = stick with { TriggerThreshold = ReadFraction(value, field, warnings) ?? stick.TriggerThreshold };
                    break;
                default:
                    warnings.Add($"{field}: not a known field, ignored");
                    break;
            }
        }

        return new BindingProfile(kind, game, menu, stick);
    }

    // Without a button set (no data folder) button names can't be checked; they are kept, not dropped.
    private static bool IsGameInput(BindingTargets targets, string input) =>
        targets.TryGetMask(input, out _) || !targets.HasButtons;

    private static bool IsMenuInput(string input) => Array.IndexOf(MenuInputs.All, input) >= 0;

    private static JsonObject WriteLayer(IReadOnlyList<ProfileBinding> bindings)
    {
        var layer = new JsonObject();
        foreach (ProfileBinding binding in bindings)
        {
            if (layer[binding.Input] is not JsonArray controls)
            {
                controls = [];
                layer[binding.Input] = controls;
            }

            controls.Add(binding.Control.Name);
        }

        return layer;
    }

    private static IReadOnlyList<ProfileBinding> ReadLayer(
        JsonNode? node,
        InputDeviceKind kind,
        bool isMenu,
        string path,
        List<string> warnings,
        Func<string, bool> isInput,
        IReadOnlyList<ProfileBinding> fallback)
    {
        if (node is not JsonObject layer)
        {
            warnings.Add($"{path}: not an object, using the default bindings");
            return fallback;
        }

        var bindings = new List<ProfileBinding>();
        foreach ((string input, JsonNode? value) in layer)
        {
            string inputPath = $"{path}.{input}";
            if (!isInput(input))
            {
                warnings.Add($"{inputPath}: '{input}' is not an input, dropped");
                continue;
            }

            if (value is not JsonArray controls)
            {
                warnings.Add($"{inputPath}: not a list of control names, dropped");
                continue;
            }

            for (int index = 0; index < controls.Count; index++)
            {
                if (ReadControl(controls[index], kind, isMenu, out BoundControl control))
                {
                    bindings.Add(new ProfileBinding(input, control));
                }
                else
                {
                    warnings.Add($"{inputPath}[{index}]: {controls[index]?.ToJsonString()} is not a {Describe(kind)}, dropped");
                }
            }
        }

        return bindings;
    }

    // Menus read keys and buttons only, not triggers.
    private static bool ReadControl(JsonNode? node, InputDeviceKind kind, bool isMenu, out BoundControl control)
    {
        control = default;
        return node is JsonValue value && value.GetValueKind() == JsonValueKind.String &&
            BoundControl.TryParse(kind, value.GetValue<string>(), out control) && (!isMenu || control.IsMenuControl);
    }

    private static string Describe(InputDeviceKind kind) => kind == InputDeviceKind.Keyboard
        ? "Godot key name (e.g. W, Up, Space, Kp4)"
        : "Godot joypad button or trigger (e.g. A, X, DpadLeft, RightShoulder, TriggerRight)";

    private static bool? ReadBool(JsonNode? node, string path, List<string> warnings)
    {
        if (node is JsonValue value && value.GetValueKind() is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetValue<bool>();
        }

        warnings.Add($"{path}: must be true or false, using the default");
        return null;
    }

    private static float? ReadFraction(JsonNode? node, string path, List<string> warnings)
    {
        if (node is JsonValue value && value.GetValueKind() == JsonValueKind.Number &&
            value.GetValue<double>() is >= 0 and <= 1 and var number)
        {
            return (float)number;
        }

        warnings.Add($"{path}: must be a number from 0 to 1, using the default");
        return null;
    }
}
