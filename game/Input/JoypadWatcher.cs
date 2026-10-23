using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace FightingGame.Input;

/// <summary>Keeps a <see cref="DeviceRoster"/> up to date as Godot reports joypads connecting and disconnecting.</summary>
public sealed class JoypadWatcher : IDisposable
{
    private readonly DeviceRoster _roster;

    public JoypadWatcher(DeviceRoster roster)
    {
        _roster = roster;
        Godot.Input.JoyConnectionChanged += OnJoyConnectionChanged;
        Refresh();
    }

    public void Dispose() => Godot.Input.JoyConnectionChanged -= OnJoyConnectionChanged;

    private void OnJoyConnectionChanged(long device, bool connected) => Refresh();

    private void Refresh()
    {
        var joypads = new List<JoypadInfo>();
        foreach (int device in Godot.Input.GetConnectedJoypads())
        {
            joypads.Add(Describe(device));
        }

        _roster.Update(joypads);
        foreach (JoypadInfo joypad in _roster.Joypads.Connected)
        {
            GD.Print($"Joypad {joypad.Device}: {JoypadText.Describe(joypad, _roster.Joypads.SlotOf(joypad.Device))}");
        }
    }

    private static JoypadInfo Describe(int device)
    {
        Dictionary info = Godot.Input.GetJoyInfo(device);
        bool ignored = info.TryGetValue("vendor_id", out Variant vendor)
            && info.TryGetValue("product_id", out Variant product)
            && Godot.Input.ShouldIgnoreDevice(vendor.AsInt32(), product.AsInt32());
        return new JoypadInfo(
            device,
            Godot.Input.GetJoyName(device),
            info.ContainsKey("steam_input_index"),
            ignored,
            Godot.Input.GetJoyGuid(device));
    }
}
