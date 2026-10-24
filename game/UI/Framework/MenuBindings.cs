using System.Collections.Generic;
using FightingGame.Input.Profiles;
using Godot;

namespace FightingGame.UI.Framework;

/// <summary>
/// Which keys and joypad buttons mean which <see cref="MenuAction"/>. <see cref="Default"/> is the one place the
/// fixed mapping is set; every gamepad uses the same one, and so do leverless controllers that report as a pad.
/// <see cref="WithProfiles"/> takes confirm and back from each device's own layout instead (UI-10, ADR-0047).
/// </summary>
/// <remarks>
/// Holding <see cref="FallbackBackKey"/> or <see cref="FallbackBackButton"/> for <see cref="FallbackHoldMilliseconds"/>
/// is back on every screen, however back was rebound, so nobody can lock themselves out.
/// </remarks>
public sealed class MenuBindings
{
    /// <summary>Developer setting: how long the default back control is held to go back whatever it is bound to.</summary>
    public const long FallbackHoldMilliseconds = 2000;

    public const Key FallbackBackKey = Key.Escape;

    public const JoyButton FallbackBackButton = JoyButton.B;

    private readonly Dictionary<Key, MenuAction> _keys;
    private readonly Dictionary<JoyButton, MenuAction> _buttons;
    private readonly IMenuProfiles? _profiles;

    public MenuBindings(
        Dictionary<Key, MenuAction> keys,
        Dictionary<JoyButton, MenuAction> buttons,
        bool leftStick,
        IMenuProfiles? profiles = null)
    {
        _keys = keys;
        _buttons = buttons;
        LeftStick = leftStick;
        _profiles = profiles;
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

    /// <summary>The same fixed mapping, with confirm and back read from each device's layout.</summary>
    public MenuBindings WithProfiles(IMenuProfiles profiles) => new(_keys, _buttons, LeftStick, profiles);

    /// <summary>By physical key, so the mapping stays put on other keyboard layouts.</summary>
    public MenuAction ForKey(Key key) =>
        Remapped(_keys.GetValueOrDefault(key), _profiles?.Keyboard, BoundControl.OfKey(key));

    /// <param name="joypad">Godot's joypad index, whose layout gives confirm and back.</param>
    public MenuAction ForButton(int joypad, JoyButton button) =>
        Remapped(_buttons.GetValueOrDefault(button), _profiles?.Joypad(joypad), BoundControl.OfButton(button));

    private static MenuAction Remapped(MenuAction fixedActions, BindingProfile? profile, BoundControl control)
    {
        if (profile is null)
        {
            return fixedActions;
        }

        MenuAction actions = fixedActions & ~(MenuAction.Confirm | MenuAction.Back);
        foreach (ProfileBinding binding in profile.Menu)
        {
            if (binding.Control == control)
            {
                actions |= binding.Input == MenuInputs.Confirm ? MenuAction.Confirm : MenuAction.Back;
            }
        }

        return actions;
    }
}
