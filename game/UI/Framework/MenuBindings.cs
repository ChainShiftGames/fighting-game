using System.Collections.Generic;
using Godot;

namespace FightingGame.UI.Framework;

/// <summary>
/// Which keys and joypad buttons mean which <see cref="MenuAction"/>. <see cref="Default"/> is the one place the
/// default mapping is set; every gamepad uses the same one, and so do leverless controllers that report as a pad.
/// </summary>
public sealed class MenuBindings
{
    private readonly Dictionary<Key, MenuAction> _keys;
    private readonly Dictionary<JoyButton, MenuAction> _buttons;

    public MenuBindings(Dictionary<Key, MenuAction> keys, Dictionary<JoyButton, MenuAction> buttons, bool leftStick)
    {
        _keys = keys;
        _buttons = buttons;
        LeftStick = leftStick;
    }

    /// <summary>
    /// The keyboard as before the menu framework (arrows, Enter or Space, Escape), plus Backspace for back and tabs on
    /// Q and E; joypads move with the d-pad or left stick, confirm on A, go back on B and pause on Start.
    /// </summary>
    public static MenuBindings Default { get; } = new(
        new Dictionary<Key, MenuAction>
        {
            [Key.Up] = MenuAction.Up,
            [Key.Down] = MenuAction.Down,
            [Key.Left] = MenuAction.Left,
            [Key.Right] = MenuAction.Right,
            [Key.Enter] = MenuAction.Confirm,
            [Key.KpEnter] = MenuAction.Confirm,
            [Key.Space] = MenuAction.Confirm,

            // Escape pauses a match and goes back in menus, as it always has.
            [Key.Escape] = MenuAction.Back | MenuAction.Start,
            [Key.Backspace] = MenuAction.Back,
            [Key.Q] = MenuAction.TabLeft,
            [Key.E] = MenuAction.TabRight,
        },
        new Dictionary<JoyButton, MenuAction>
        {
            [JoyButton.DpadUp] = MenuAction.Up,
            [JoyButton.DpadDown] = MenuAction.Down,
            [JoyButton.DpadLeft] = MenuAction.Left,
            [JoyButton.DpadRight] = MenuAction.Right,
            [JoyButton.A] = MenuAction.Confirm,
            [JoyButton.B] = MenuAction.Back,
            [JoyButton.LeftShoulder] = MenuAction.TabLeft,
            [JoyButton.RightShoulder] = MenuAction.TabRight,
            [JoyButton.Start] = MenuAction.Start,
        },
        leftStick: true);

    /// <summary>Whether a joypad's left stick moves the cursor too.</summary>
    public bool LeftStick { get; }

    /// <summary>By physical key, so the mapping stays put on other keyboard layouts.</summary>
    public MenuAction ForKey(Key key) => _keys.GetValueOrDefault(key);

    public MenuAction ForButton(JoyButton button) => _buttons.GetValueOrDefault(button);
}
