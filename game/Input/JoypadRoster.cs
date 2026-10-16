using System;
using System.Collections.Generic;

namespace FightingGame.Input;

/// <summary>Which joypad each player slot reads, so every physical controller gives exactly one player input.</summary>
/// <remarks>
/// The no-double-input rule: with Steam Input on, Steam turns a physical controller into a virtual pad and puts the
/// physical one on SDL's ignore list, but Godot may still report both. A joypad on that list is never used, so only
/// the virtual pad is read. Every remaining joypad fills exactly one slot, lowest Godot index first, so no
/// controller can drive two players.
/// </remarks>
public sealed class JoypadRoster
{
    private JoypadInfo[] _connected = [];
    private int[] _slots = [];

    /// <summary>Every connected joypad, used or not, lowest Godot index first.</summary>
    public IReadOnlyList<JoypadInfo> Connected => _connected;

    /// <summary>Call whenever a joypad connects or disconnects.</summary>
    public void Update(IEnumerable<JoypadInfo> connected)
    {
        var all = new List<JoypadInfo>(connected);
        all.Sort((left, right) => left.Device.CompareTo(right.Device));
        var slots = new List<int>(all.Count);
        foreach (JoypadInfo joypad in all)
        {
            if (!joypad.IgnoredBySteam && !slots.Contains(joypad.Device))
            {
                slots.Add(joypad.Device);
            }
        }

        _connected = [.. all];
        _slots = [.. slots];
    }

    /// <summary>The Godot joypad index for <paramref name="slot"/>; false while no joypad fills it.</summary>
    public bool TryGetDevice(int slot, out int device)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        bool filled = slot < _slots.Length;
        device = filled ? _slots[slot] : -1;
        return filled;
    }

    /// <summary>The slot <paramref name="device"/> fills, or -1 when it isn't used.</summary>
    public int SlotOf(int device) => Array.IndexOf(_slots, device);
}
