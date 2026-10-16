using System.Collections.Generic;
using FightingGame.Input;
using Godot;

namespace FightingGame.Tests.Input;

/// <summary>Device state set by the test instead of read from hardware.</summary>
internal sealed class FakeDeviceState : IDeviceState
{
    private readonly HashSet<Key> _keys = [];
    private readonly HashSet<(int, JoyButton)> _buttons = [];
    private readonly Dictionary<(int, JoyAxis), float> _axes = [];

    public FakeDeviceState Hold(params Key[] keys)
    {
        _keys.UnionWith(keys);
        return this;
    }

    public FakeDeviceState Hold(int device, params JoyButton[] buttons)
    {
        foreach (JoyButton button in buttons)
        {
            _buttons.Add((device, button));
        }

        return this;
    }

    public FakeDeviceState Move(int device, JoyAxis axis, float value)
    {
        _axes[(device, axis)] = value;
        return this;
    }

    public bool IsKeyHeld(Key key) => _keys.Contains(key);

    public bool IsJoyButtonHeld(int device, JoyButton button) => _buttons.Contains((device, button));

    public float JoyAxisValue(int device, JoyAxis axis) => _axes.GetValueOrDefault((device, axis));
}
