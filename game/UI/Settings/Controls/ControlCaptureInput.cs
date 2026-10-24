using FightingGame.Input;
using FightingGame.Input.Profiles;
using Godot;

namespace FightingGame.UI.Settings.Controls;

/// <summary>
/// Hands raw presses to the controls menu while it waits for a control (<see cref="ControlsMenu.Offer"/>). It reads
/// input before the menus do, and a press the capture takes is marked handled, so it never also moves a menu.
/// </summary>
/// <remarks>Only joypads in the roster count, keeping its no-double-input rule with Steam Input.</remarks>
public partial class ControlCaptureInput : Node
{
    private ControlsMenu? _menu;
    private DeviceRoster? _roster;

    public void Bind(ControlsMenu menu, DeviceRoster roster)
    {
        _menu = menu;
        _roster = roster;
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (_menu?.Capture is null || !TryRead(inputEvent, out InputDeviceId device, out BoundControl control))
        {
            return;
        }

        if (_menu.Offer(device, control))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    private bool TryRead(InputEvent inputEvent, out InputDeviceId device, out BoundControl control)
    {
        device = InputDeviceId.Keyboard;
        control = default;
        switch (inputEvent)
        {
            case InputEventKey { Pressed: true, Echo: false } key when key.PhysicalKeycode != Key.None:
                control = BoundControl.OfKey(key.PhysicalKeycode);
                return true;
            case InputEventJoypadButton { Pressed: true } button when _roster!.TryFindJoypad(button.Device, out InputDeviceInfo pad):
                device = pad.Id;
                control = BoundControl.OfButton(button.ButtonIndex);
                return true;
            case InputEventJoypadMotion { Axis: JoyAxis.TriggerLeft or JoyAxis.TriggerRight } motion
                when _roster!.TryFindJoypad(motion.Device, out InputDeviceInfo pad) &&
                motion.AxisValue > _menu!.Profile.Stick.TriggerThreshold:
                device = pad.Id;
                control = BoundControl.OfTrigger(motion.Axis);
                return true;
            default:
                return false;
        }
    }
}
