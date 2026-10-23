using System;
using System.Collections.Generic;
using FightingGame.Input;
using Godot;

namespace FightingGame.UI.Framework;

/// <summary>
/// Reads menu presses from the keyboard and every used joypad (<see cref="MenuActionReader"/>) and hands each to a
/// handler; a press the handler takes is marked handled, so nodes before this one never see it.
/// </summary>
/// <remarks>
/// Only joypads that fill a <see cref="JoypadRoster"/> slot are read, keeping the roster's no-double-input rule with
/// Steam Input. Presses come from unhandled events, so menus opened above this node (options, training) take theirs
/// first, as before.
/// </remarks>
public partial class MenuInput : Node
{
    private readonly List<MenuPress> _repeats = [];
    private MenuActionReader? _reader;
    private IDeviceState? _devices;
    private JoypadRoster? _joypads;
    private Func<MenuPress, bool>? _handler;

    /// <param name="handler">Acts on a press; returns whether it used it.</param>
    public void Bind(MenuActionReader reader, IDeviceState devices, JoypadRoster joypads, Func<MenuPress, bool> handler)
    {
        _reader = reader;
        _devices = devices;
        _joypads = joypads;
        _handler = handler;
    }

    /// <summary>Whether any device holds a control that means one of <paramref name="actions"/>.</summary>
    public bool IsHeld(MenuAction actions) => _reader?.IsHeld(actions) ?? false;

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (_reader is null)
        {
            return;
        }

        long now = (long)Time.GetTicksMsec();
        MenuPress? press = inputEvent switch
        {
            InputEventKey { Echo: false } key => _reader.KeyChanged(key.PhysicalKeycode, key.Pressed, now),
            InputEventJoypadButton button when IsUsed(button.Device) =>
                _reader.ButtonChanged(button.Device, button.ButtonIndex, button.Pressed, now),
            InputEventJoypadMotion motion when IsUsed(motion.Device) =>
                _reader.StickMoved(motion.Device, motion.Axis, motion.AxisValue, now),
            _ => null,
        };
        if (press is { } pressed && _handler!(pressed))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (_reader is null)
        {
            return;
        }

        _reader.ForgetLetGo(_devices!);
        _repeats.Clear();
        _reader.AddRepeats((long)Time.GetTicksMsec(), _repeats);
        foreach (MenuPress press in _repeats)
        {
            _handler!(press);
        }
    }

    private bool IsUsed(int device) => _joypads!.SlotOf(device) >= 0;
}
