using System;
using System.Collections.Generic;

namespace FightingGame.Input;

/// <summary>
/// Every usable input device with a stable id: the keyboard, always there, then each joypad the
/// <see cref="JoypadRoster"/> uses, lowest Godot index first (ADR-0038).
/// </summary>
/// <remarks>
/// The no-double-input rule carries over from <see cref="JoypadRoster"/>: a physical controller Steam Input
/// re-presents is never listed, so each controller appears exactly once.
/// </remarks>
public sealed class DeviceRoster
{
    private static readonly InputDeviceInfo _keyboard =
        new(InputDeviceId.Keyboard, "Keyboard", InputDeviceInfo.NoJoypad);

    private InputDeviceInfo[] _devices = [_keyboard];

    /// <summary>A device joined the roster; raised after the roster changed.</summary>
    public event Action<InputDeviceInfo>? Connected;

    /// <summary>A device left the roster; raised after the roster changed. Players keep it assigned.</summary>
    public event Action<InputDeviceInfo>? Disconnected;

    /// <summary>The keyboard, then the joypads in Godot's order.</summary>
    public IReadOnlyList<InputDeviceInfo> Devices => _devices;

    /// <summary>Every joypad Godot reports, used or not, for the log and the developer overlay.</summary>
    public JoypadRoster Joypads { get; } = new();

    /// <summary>Call whenever a joypad connects or disconnects.</summary>
    public void Update(IEnumerable<JoypadInfo> connected)
    {
        Joypads.Update(connected);
        List<JoypadInfo> used = UsedJoypads();
        var ids = new InputDeviceId?[used.Count];

        // Pads still connected keep their id first, so a pad that just arrived can't take one of theirs.
        for (int index = 0; index < used.Count; index++)
        {
            ids[index] = StillConnected(used[index]);
        }

        for (int index = 0; index < used.Count; index++)
        {
            ids[index] ??= InputDeviceId.Joypad(used[index].Guid, FreeOrdinal(used[index].Guid, ids));
        }

        InputDeviceInfo[] previous = _devices;
        var devices = new List<InputDeviceInfo> { _keyboard };
        for (int index = 0; index < used.Count; index++)
        {
            devices.Add(new InputDeviceInfo(ids[index]!.Value, used[index].Name, used[index].Device));
        }

        _devices = [.. devices];
        RaiseChanges(previous);
    }

    public bool IsConnected(InputDeviceId id) => IndexOf(_devices, id) >= 0;

    /// <summary>Godot's joypad index for <paramref name="id"/>; false for the keyboard or a disconnected pad.</summary>
    public bool TryGetJoypad(InputDeviceId id, out int joypad)
    {
        int index = IndexOf(_devices, id);
        joypad = index >= 0 ? _devices[index].Joypad : InputDeviceInfo.NoJoypad;
        return joypad != InputDeviceInfo.NoJoypad;
    }

    /// <summary>The roster entry for Godot's joypad <paramref name="joypad"/>; false when the roster doesn't use it.</summary>
    public bool TryFindJoypad(int joypad, out InputDeviceInfo device)
    {
        foreach (InputDeviceInfo candidate in _devices)
        {
            if (candidate.Id.IsJoypad && candidate.Joypad == joypad)
            {
                device = candidate;
                return true;
            }
        }

        device = default;
        return false;
    }

    private List<JoypadInfo> UsedJoypads()
    {
        var used = new List<JoypadInfo>();
        foreach (JoypadInfo joypad in Joypads.Connected)
        {
            if (Joypads.SlotOf(joypad.Device) >= 0)
            {
                used.Add(joypad);
            }
        }

        return used;
    }

    // Godot keeps a pad's index while it stays connected; the same index with another model is another pad.
    private InputDeviceId? StillConnected(JoypadInfo joypad)
    {
        foreach (InputDeviceInfo device in _devices)
        {
            if (device.Id.IsJoypad && device.Joypad == joypad.Device && device.Id.Guid == joypad.Guid)
            {
                return device.Id;
            }
        }

        return null;
    }

    // The lowest ordinal of that model not in use: a pad that comes back while the other pads of its model stayed
    // connected gets its own ordinal again.
    private static int FreeOrdinal(string guid, InputDeviceId?[] taken)
    {
        int ordinal = 0;
        while (Array.IndexOf(taken, InputDeviceId.Joypad(guid, ordinal)) >= 0)
        {
            ordinal++;
        }

        return ordinal;
    }

    private void RaiseChanges(InputDeviceInfo[] previous)
    {
        foreach (InputDeviceInfo device in previous)
        {
            if (IndexOf(_devices, device.Id) < 0)
            {
                Disconnected?.Invoke(device);
            }
        }

        foreach (InputDeviceInfo device in _devices)
        {
            if (IndexOf(previous, device.Id) < 0)
            {
                Connected?.Invoke(device);
            }
        }
    }

    // Read before each logic frame, so a plain loop: no allocation.
    private static int IndexOf(InputDeviceInfo[] devices, InputDeviceId id)
    {
        for (int index = 0; index < devices.Length; index++)
        {
            if (devices[index].Id == id)
            {
                return index;
            }
        }

        return -1;
    }
}
