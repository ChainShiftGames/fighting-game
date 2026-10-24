using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Sim.Input;

namespace FightingGame.Input;

/// <summary>
/// Reads the old per-player dev bindings file (<c>user://dev_bindings.jsonc</c>, schema version 1: a <c>players</c>
/// list, each with <c>keyboard</c> and <c>gamepad</c> bindings) so <see cref="Profiles.DevBindingsMigration"/> can move
/// it to per-device layouts once (ADR-0047).
/// </summary>
/// <remarks>
/// Every problem is reported with its place in the file; a file with any problem gives no bindings, so a typo never
/// leaves a button silently unbound.
/// </remarks>
public sealed class BindingsParser
{
    public const int SchemaVersion = 1;

    /// <summary>Players the old file seats: two for local matches.</summary>
    public const int PlayerCount = 2;

    private static readonly JsonDocumentOptions _json = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    private readonly BindingTargets _targets;
    private readonly int _playerCount;
    private readonly List<string> _errors = [];

    /// <param name="buttons">The button set; null when there is none, and button inputs are then skipped.</param>
    /// <param name="playerCount">How many players the file must bind.</param>
    public BindingsParser(ButtonSet? buttons, int playerCount)
    {
        _targets = new BindingTargets(buttons);
        _playerCount = playerCount;
    }

    /// <summary>Problems found by the last <see cref="Parse"/>, each starting with where in the file it is.</summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>One entry per player, in player order; null when <see cref="Errors"/> isn't empty.</summary>
    public PlayerBindings[]? Parse(string json)
    {
        _errors.Clear();
        try
        {
            using JsonDocument document = JsonDocument.Parse(json, _json);
            PlayerBindings[] players = ReadFile(document.RootElement);
            return _errors.Count == 0 ? players : null;
        }
        catch (JsonException exception)
        {
            _errors.Add($"not valid JSON: {exception.Message}");
            return null;
        }
    }

    private PlayerBindings[] ReadFile(JsonElement root)
    {
        var file = new JsonFields(root, "file", _errors);
        if (file.Int("schemaVersion") is int version && version != SchemaVersion)
        {
            file.Error("schemaVersion", $"is {version}, this game reads version {SchemaVersion}");
        }

        var players = new List<PlayerBindings>();
        foreach ((JsonElement player, string path) in file.Array("players"))
        {
            players.Add(ReadPlayer(new JsonFields(player, path, _errors)));
        }

        if (players.Count != _playerCount)
        {
            file.Error("players", $"lists {players.Count} players; it needs exactly {_playerCount}");
        }

        file.Finish();
        return [.. players];
    }

    private PlayerBindings ReadPlayer(JsonFields player)
    {
        KeyboardBindings keyboard = ReadKeyboard(player.Object("keyboard"));
        GamepadBindings gamepad = ReadGamepad(player.Object("gamepad"));
        player.Finish();
        return new PlayerBindings(keyboard, gamepad);
    }

    private KeyboardBindings ReadKeyboard(JsonFields keyboard)
    {
        var keys = new List<KeyBinding>();
        foreach ((string control, ushort mask, string path) in ReadBindings(keyboard))
        {
            if (TryParseName(control, out Key key) && key != Key.None)
            {
                keys.Add(new KeyBinding(key, mask));
            }
            else
            {
                _errors.Add($"{path}: '{control}' is not a Godot key name (e.g. W, Up, Space, Kp4)");
            }
        }

        keyboard.Finish();
        return new KeyboardBindings([.. keys]);
    }

    private GamepadBindings ReadGamepad(JsonFields gamepad)
    {
        bool leftStick = gamepad.Bool("leftStick") ?? false;
        float stickDeadzone = gamepad.Fraction("stickDeadzone") ?? 0;
        float triggerThreshold = gamepad.Fraction("triggerThreshold") ?? 0;
        var buttons = new List<PadButtonBinding>();
        var triggers = new List<PadTriggerBinding>();
        foreach ((string control, ushort mask, string path) in ReadBindings(gamepad))
        {
            if (TryParseName(control, out JoyAxis axis) && axis is JoyAxis.TriggerLeft or JoyAxis.TriggerRight)
            {
                triggers.Add(new PadTriggerBinding(axis, mask));
            }
            else if (TryParseName(control, out JoyButton button) && button is > JoyButton.Invalid and < JoyButton.SdlMax)
            {
                buttons.Add(new PadButtonBinding(button, mask));
            }
            else
            {
                _errors.Add(
                    $"{path}: '{control}' is not a Godot joypad button or trigger (e.g. A, X, DpadLeft, " +
                    "RightShoulder, TriggerRight)");
            }
        }

        gamepad.Finish();
        return new GamepadBindings([.. buttons], [.. triggers], leftStick, stickDeadzone, triggerThreshold);
    }

    // The "bindings" object: input name → list of control names. Unbound inputs are simply left out.
    private List<(string Control, ushort Mask, string Path)> ReadBindings(JsonFields device)
    {
        var bindings = new List<(string, ushort, string)>();
        JsonFields inputs = device.Object("bindings");
        foreach ((string input, JsonElement value, string inputPath) in inputs.Remaining())
        {
            if (!_targets.TryGetMask(input, out ushort mask))
            {
                if (!_targets.HasButtons)
                {
                    // Already reported once at startup: without a button set no button can be bound.
                    continue;
                }

                _errors.Add($"{inputPath}: '{input}' is not an input; use one of {_targets.Describe()}");
                continue;
            }

            foreach ((string control, string path) in JsonFields.Strings(value, inputPath, _errors))
            {
                bindings.Add((control, mask, path));
            }
        }

        return bindings;
    }

    // Exact enum member names only: Enum.TryParse alone would also accept numbers and comma lists.
    private static bool TryParseName<TEnum>(string name, out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;
        return Array.IndexOf(Enum.GetNames<TEnum>(), name) >= 0 && Enum.TryParse(name, out value);
    }
}
