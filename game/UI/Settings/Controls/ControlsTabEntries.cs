using System.Collections;
using System.Collections.Generic;
using FightingGame.Input;

namespace FightingGame.UI.Settings.Controls;

/// <summary>The Controls tab's entries: one per device connected now, so a pad plugged in while the menu is open shows up.</summary>
public sealed class ControlsTabEntries : IReadOnlyList<ISettingEntry>
{
    private readonly ControlsMenu _menu;
    private readonly DeviceRoster _roster;
    private readonly bool _isOverMatch;

    /// <param name="isOverMatch">The options menu is open from the pause menu, over the match.</param>
    public ControlsTabEntries(ControlsMenu menu, DeviceRoster roster, bool isOverMatch)
    {
        _menu = menu;
        _roster = roster;
        _isOverMatch = isOverMatch;
    }

    public int Count => _roster.Devices.Count;

    public ISettingEntry this[int index] => new DeviceControlsEntry(_menu, _roster.Devices[index], _isOverMatch);

    public IEnumerator<ISettingEntry> GetEnumerator()
    {
        // A snapshot: the roster may change between two reads of the tab.
        IReadOnlyList<InputDeviceInfo> devices = _roster.Devices;
        foreach (InputDeviceInfo device in devices)
        {
            yield return new DeviceControlsEntry(_menu, device, _isOverMatch);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
