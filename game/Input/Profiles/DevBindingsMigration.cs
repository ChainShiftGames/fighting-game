using System.Collections.Generic;

namespace FightingGame.Input.Profiles;

/// <summary>
/// Turns the old per-player dev bindings (<c>user://dev_bindings.jsonc</c>, read by <see cref="BindingsParser"/>) into
/// per-device layouts once, losing nothing (ADR-0047 §4): player 1's keys become the keyboard's layout and player 1's
/// pad bindings the default for every pad. Player 2's keyboard and pad layouts, where they differ from player 1's and
/// from every shipped preset, are kept as presets a player can load.
/// </summary>
/// <remarks>The old file had no menu bindings: the migrated layouts get their kind's default menu buttons.</remarks>
public static class DevBindingsMigration
{
    public const string Player2KeyboardPresetId = "old_player2_keyboard";
    public const string Player2JoypadPresetId = "old_player2_joypad";
    private const string Player2KeyboardName = "MENU_CONTROLS_PRESET_OLD_P2_KEYBOARD";
    private const string Player2JoypadName = "MENU_CONTROLS_PRESET_OLD_P2_PAD";

    /// <param name="players">The old file's players, player 1 first.</param>
    public static ControlsFile Migrate(IReadOnlyList<PlayerBindings> players, BindingTargets targets, BuiltInPresets builtIn)
    {
        BindingProfile keyboardMenu = builtIn.DefaultOf(InputDeviceKind.Keyboard);
        BindingProfile joypadMenu = builtIn.DefaultOf(InputDeviceKind.Joypad);
        BindingProfile keyboard = FromKeyboard(players[0].Keyboard, targets, keyboardMenu);
        BindingProfile joypad = FromGamepad(players[0].Gamepad, targets, joypadMenu);
        var kept = new List<ControlPreset>();
        if (players.Count > 1)
        {
            KeepIfNew(FromKeyboard(players[1].Keyboard, targets, keyboardMenu), keyboard, Player2KeyboardPresetId, Player2KeyboardName, builtIn, kept);
            KeepIfNew(FromGamepad(players[1].Gamepad, targets, joypadMenu), joypad, Player2JoypadPresetId, Player2JoypadName, builtIn, kept);
        }

        return new ControlsFile(keyboard, joypad, [], kept);
    }

    private static void KeepIfNew(
        BindingProfile layout, BindingProfile player1, string id, string name, BuiltInPresets builtIn, List<ControlPreset> kept)
    {
        if (layout.SameAs(player1))
        {
            return;
        }

        foreach (ControlPreset preset in builtIn.All)
        {
            if (layout.SameAs(preset.Profile))
            {
                return;
            }
        }

        kept.Add(new ControlPreset(id, name, layout));
    }

    private static BindingProfile FromKeyboard(KeyboardBindings keyboard, BindingTargets targets, BindingProfile menuFrom)
    {
        var game = new List<ProfileBinding>();
        foreach (KeyBinding key in keyboard.Keys)
        {
            Add(game, key.Mask, BoundControl.OfKey(key.Key), targets);
        }

        return new BindingProfile(InputDeviceKind.Keyboard, game, menuFrom.Menu, StickOptions.None);
    }

    private static BindingProfile FromGamepad(GamepadBindings gamepad, BindingTargets targets, BindingProfile menuFrom)
    {
        var game = new List<ProfileBinding>();
        foreach (PadButtonBinding button in gamepad.Buttons)
        {
            Add(game, button.Mask, BoundControl.OfButton(button.Button), targets);
        }

        foreach (PadTriggerBinding trigger in gamepad.Triggers)
        {
            Add(game, trigger.Mask, BoundControl.OfTrigger(trigger.Trigger), targets);
        }

        var stick = new StickOptions(gamepad.LeftStick, gamepad.StickDeadzone, gamepad.TriggerThreshold);
        return new BindingProfile(InputDeviceKind.Joypad, game, menuFrom.Menu, stick);
    }

    // Each old binding set exactly one input's bit, so the bit names the input again.
    private static void Add(List<ProfileBinding> game, ushort mask, BoundControl control, BindingTargets targets)
    {
        if (targets.TryGetName(mask, out string input))
        {
            game.Add(new ProfileBinding(input, control));
        }
    }
}
