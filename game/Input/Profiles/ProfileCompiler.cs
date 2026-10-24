using System.Collections.Generic;
using Godot;

namespace FightingGame.Input.Profiles;

/// <summary>
/// Turns a profile's game bindings into the bindings the device readers use: input names become input word bits
/// through <see cref="BindingTargets"/>. Done when a match starts (and by the menu's test area), never per frame.
/// </summary>
public sealed class ProfileCompiler
{
    private readonly BindingTargets _targets;

    public ProfileCompiler(BindingTargets targets)
    {
        _targets = targets;
    }

    public BindingTargets Targets => _targets;

    public KeyboardBindings Keyboard(BindingProfile profile)
    {
        var keys = new List<KeyBinding>();
        foreach (ProfileBinding binding in profile.Game)
        {
            if (binding.Control.Kind == BoundControlKind.Key && _targets.TryGetMask(binding.Input, out ushort mask))
            {
                keys.Add(new KeyBinding((Key)binding.Control.Code, mask));
            }
        }

        return new KeyboardBindings([.. keys]);
    }

    public GamepadBindings Gamepad(BindingProfile profile)
    {
        var buttons = new List<PadButtonBinding>();
        var triggers = new List<PadTriggerBinding>();
        foreach (ProfileBinding binding in profile.Game)
        {
            if (!_targets.TryGetMask(binding.Input, out ushort mask))
            {
                continue;
            }

            if (binding.Control.Kind == BoundControlKind.PadButton)
            {
                buttons.Add(new PadButtonBinding((JoyButton)binding.Control.Code, mask));
            }
            else if (binding.Control.Kind == BoundControlKind.PadTrigger)
            {
                triggers.Add(new PadTriggerBinding((JoyAxis)binding.Control.Code, mask));
            }
        }

        StickOptions stick = profile.Stick;
        return new GamepadBindings([.. buttons], [.. triggers], stick.LeftStick, stick.StickDeadzone, stick.TriggerThreshold);
    }
}
