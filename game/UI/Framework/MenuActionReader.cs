using System;
using System.Collections.Generic;
using FightingGame.Input;
using Godot;

namespace FightingGame.UI.Framework;

/// <summary>
/// Turns key, joypad button and stick changes into <see cref="MenuPress"/>es through <see cref="MenuBindings"/>, and
/// repeats held directions (<see cref="MenuTiming"/>) so long lists scroll.
/// </summary>
public sealed class MenuActionReader
{
    private static readonly int[] _stickSides = [-1, 1];

    private readonly MenuBindings _bindings;
    private readonly MenuTiming _timing;
    private readonly Dictionary<HeldControl, Hold> _held = [];
    private readonly List<HeldControl> _letGo = [];

    public MenuActionReader(MenuBindings bindings, MenuTiming timing)
    {
        _bindings = bindings;
        _timing = timing;
    }

    private enum ControlKind
    {
        Key,
        PadButton,
        Stick,
    }

    /// <summary>A key went down or up; returns the press it makes, if any.</summary>
    public MenuPress? KeyChanged(Key key, bool pressed, long nowMilliseconds) => Change(
        new HeldControl(MenuDevice.Keyboard, ControlKind.Key, (int)key), _bindings.ForKey(key), pressed, nowMilliseconds);

    public MenuPress? ButtonChanged(int device, JoyButton button, bool pressed, long nowMilliseconds) => Change(
        new HeldControl(MenuDevice.Gamepad(device), ControlKind.PadButton, (int)button),
        _bindings.ForButton(button),
        pressed,
        nowMilliseconds);

    /// <summary>A stick axis moved (−1 to 1, Y points down); returns the press of a direction it entered, if any.</summary>
    public MenuPress? StickMoved(int device, JoyAxis axis, float value, long nowMilliseconds)
    {
        if (!_bindings.LeftStick || axis is not (JoyAxis.LeftX or JoyAxis.LeftY))
        {
            return null;
        }

        MenuPress? press = null;
        foreach (int side in _stickSides)
        {
            var control = new HeldControl(MenuDevice.Gamepad(device), ControlKind.Stick, StickCode(axis, side));
            bool pushed = side * value > _timing.StickDeadzone;
            press = Change(control, StickAction(axis, side), pushed, nowMilliseconds) ?? press;
        }

        return press;
    }

    /// <summary>Adds the repeats due now of every held direction to <paramref name="presses"/>, one per control.</summary>
    public void AddRepeats(long nowMilliseconds, List<MenuPress> presses)
    {
        foreach ((HeldControl control, Hold hold) in _held)
        {
            MenuAction directions = hold.Actions & MenuAction.Directions;
            if (directions == MenuAction.None || nowMilliseconds < hold.NextRepeat)
            {
                continue;
            }

            // At most one repeat per call: a slow frame never makes the cursor jump.
            hold.NextRepeat = Math.Max(hold.NextRepeat + _timing.RepeatIntervalMilliseconds, nowMilliseconds);
            presses.Add(new MenuPress(directions, control.Device, IsRepeat: true));
        }
    }

    /// <summary>Forgets controls the devices no longer hold, whose release another node took.</summary>
    public void ForgetLetGo(IDeviceState devices)
    {
        _letGo.Clear();
        foreach (HeldControl control in _held.Keys)
        {
            if (!IsStillHeld(control, devices))
            {
                _letGo.Add(control);
            }
        }

        foreach (HeldControl control in _letGo)
        {
            _held.Remove(control);
        }
    }

    /// <summary>Whether any device holds a control that means one of <paramref name="actions"/>.</summary>
    public bool IsHeld(MenuAction actions)
    {
        foreach (Hold hold in _held.Values)
        {
            if ((hold.Actions & actions) != 0)
            {
                return true;
            }
        }

        return false;
    }

    private static MenuAction StickAction(JoyAxis axis, int side) => (axis, side) switch
    {
        (JoyAxis.LeftX, < 0) => MenuAction.Left,
        (JoyAxis.LeftX, _) => MenuAction.Right,
        (_, < 0) => MenuAction.Up,
        _ => MenuAction.Down,
    };

    private static int StickCode(JoyAxis axis, int side) => ((int)axis * 2) + (side > 0 ? 1 : 0);

    private MenuPress? Change(HeldControl control, MenuAction actions, bool pressed, long nowMilliseconds)
    {
        if (actions == MenuAction.None)
        {
            return null;
        }

        if (!pressed)
        {
            _held.Remove(control);
            return null;
        }

        if (_held.ContainsKey(control))
        {
            // Already down (an OS key echo, or a stick moving within one side): repeats come from AddRepeats.
            return null;
        }

        _held[control] = new Hold(actions, nowMilliseconds + _timing.RepeatDelayMilliseconds);
        return new MenuPress(actions, control.Device);
    }

    private bool IsStillHeld(HeldControl control, IDeviceState devices)
    {
        switch (control.Kind)
        {
            case ControlKind.Key:
                return devices.IsKeyHeld((Key)control.Code);
            case ControlKind.PadButton:
                return devices.IsJoyButtonHeld(control.Device.Index, (JoyButton)control.Code);
            default:
                var axis = (JoyAxis)(control.Code / 2);
                int side = control.Code % 2 == 1 ? 1 : -1;
                return side * devices.JoyAxisValue(control.Device.Index, axis) > _timing.StickDeadzone;
        }
    }

    private readonly record struct HeldControl(MenuDevice Device, ControlKind Kind, int Code);

    private sealed class Hold(MenuAction actions, long nextRepeat)
    {
        public MenuAction Actions { get; } = actions;

        public long NextRepeat { get; set; } = nextRepeat;
    }
}
