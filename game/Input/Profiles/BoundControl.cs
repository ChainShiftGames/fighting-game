using System;
using Godot;

namespace FightingGame.Input.Profiles;

/// <summary>
/// One physical control a profile binds: a key, a joypad button or a trigger. Saved by its Godot name (<c>W</c>,
/// <c>Kp4</c>, <c>DpadLeft</c>, <c>TriggerRight</c>), the same names the old bindings file used.
/// </summary>
/// <param name="Code">The <see cref="Key"/>, <see cref="JoyButton"/> or <see cref="JoyAxis"/> value.</param>
public readonly record struct BoundControl(BoundControlKind Kind, long Code)
{
    public static BoundControl OfKey(Key key) => new(BoundControlKind.Key, (long)key);

    public static BoundControl OfButton(JoyButton button) => new(BoundControlKind.PadButton, (long)button);

    public static BoundControl OfTrigger(JoyAxis trigger) => new(BoundControlKind.PadTrigger, (long)trigger);

    /// <summary>The Godot name it is saved and shown by.</summary>
    public string Name => Kind switch
    {
        BoundControlKind.Key => ((Key)Code).ToString(),
        BoundControlKind.PadButton => ((JoyButton)Code).ToString(),
        _ => ((JoyAxis)Code).ToString(),
    };

    /// <summary>Whether menus can use it: keys and buttons; triggers are axes the menus don't read.</summary>
    public bool IsMenuControl => Kind != BoundControlKind.PadTrigger;

    /// <summary>Reads a saved name for a device of <paramref name="device"/>'s kind; false for any other name.</summary>
    public static bool TryParse(InputDeviceKind device, string name, out BoundControl control)
    {
        control = default;
        if (device == InputDeviceKind.Keyboard)
        {
            if (TryParseName(name, out Key key) && key != Key.None)
            {
                control = OfKey(key);
                return true;
            }

            return false;
        }

        if (TryParseName(name, out JoyAxis axis) && axis is JoyAxis.TriggerLeft or JoyAxis.TriggerRight)
        {
            control = OfTrigger(axis);
            return true;
        }

        if (TryParseName(name, out JoyButton button) && button is > JoyButton.Invalid and < JoyButton.SdlMax)
        {
            control = OfButton(button);
            return true;
        }

        return false;
    }

    // Exact enum member names only: Enum.TryParse alone would also accept numbers and comma lists.
    private static bool TryParseName<TEnum>(string name, out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;
        return Array.IndexOf(Enum.GetNames<TEnum>(), name) >= 0 && Enum.TryParse(name, out value);
    }
}
