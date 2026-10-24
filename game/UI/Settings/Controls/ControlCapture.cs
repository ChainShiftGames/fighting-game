using FightingGame.Input;
using FightingGame.Input.Profiles;

namespace FightingGame.UI.Settings.Controls;

/// <summary>
/// One "press a control" wait: which input of which layer gets the next control the edited device presses. Other
/// devices are ignored, so a second player can still cancel, and menus never take a trigger.
/// </summary>
/// <param name="Replace">Whether the control replaces the input's others, or is added next to them.</param>
/// <param name="Deadline">When it gives up, on the menu clock.</param>
public sealed record ControlCapture(
    InputDeviceId Device, BindingLayer Layer, string Input, bool Replace, long Deadline)
{
    public bool Accepts(InputDeviceId device, BoundControl control) =>
        device == Device && (Layer == BindingLayer.Game || control.IsMenuControl);
}
