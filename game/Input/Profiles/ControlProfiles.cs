using System;
using System.Collections.Generic;

namespace FightingGame.Input.Profiles;

/// <summary>
/// Every device's layout (ADR-0047): the one keyboard's, each joypad's own by its id, and the default a joypad without
/// one uses. A layout belongs to the device, so it follows the device to whichever side it plays on.
/// </summary>
public sealed class ControlProfiles
{
    private readonly BuiltInPresets _builtIn;
    private readonly List<ControlPreset> _kept;
    private readonly Dictionary<InputDeviceId, StoredJoypadProfile> _joypads = [];
    private readonly BindingProfile? _joypadDefault;
    private BindingProfile? _keyboard;

    public ControlProfiles(BuiltInPresets builtIn, ControlsFile saved)
    {
        _builtIn = builtIn;
        _keyboard = saved.Keyboard;
        _joypadDefault = saved.JoypadDefault;
        _kept = [.. saved.Presets];
        foreach (StoredJoypadProfile joypad in saved.Joypads)
        {
            _joypads[joypad.Device] = joypad;
        }
    }

    /// <summary>Counts changes, so readers can tell their compiled bindings are out of date.</summary>
    public int Changes { get; private set; }

    /// <summary>The layout <paramref name="device"/> plays with: its own, else its kind's default.</summary>
    public BindingProfile For(InputDeviceId device)
    {
        if (!device.IsJoypad)
        {
            return _keyboard ?? _builtIn.DefaultOf(InputDeviceKind.Keyboard);
        }

        return _joypads.TryGetValue(device, out StoredJoypadProfile? joypad)
            ? joypad.Profile
            : _joypadDefault ?? _builtIn.DefaultOf(InputDeviceKind.Joypad);
    }

    /// <summary>The shipped default of <paramref name="kind"/>, which "Reset to default" goes back to.</summary>
    public BindingProfile DefaultOf(InputDeviceKind kind) => _builtIn.DefaultOf(kind);

    /// <summary>Gives <paramref name="device"/> its own layout from now on.</summary>
    public void Set(InputDeviceInfo device, BindingProfile profile)
    {
        if (profile.Kind != device.Id.Kind)
        {
            throw new ArgumentException($"A {profile.Kind} layout can't go on {device.Name}.", nameof(profile));
        }

        if (device.Id.IsJoypad)
        {
            _joypads[device.Id] = new StoredJoypadProfile(device.Id, device.Name, profile);
        }
        else
        {
            _keyboard = profile;
        }

        Changes++;
    }

    public void ResetToDefault(InputDeviceInfo device) => Set(device, DefaultOf(device.Id.Kind));

    /// <summary>The layouts a device of <paramref name="kind"/> can load: the shipped ones, then the kept ones.</summary>
    public List<ControlPreset> PresetsFor(InputDeviceKind kind)
    {
        var presets = new List<ControlPreset>();
        foreach (ControlPreset preset in _builtIn.All)
        {
            if (preset.Profile.Kind == kind)
            {
                presets.Add(preset);
            }
        }

        foreach (ControlPreset preset in _kept)
        {
            if (preset.Profile.Kind == kind)
            {
                presets.Add(preset);
            }
        }

        return presets;
    }

    /// <summary>What to save; joypads in a fixed order, so an unchanged layout saves an unchanged file.</summary>
    public ControlsFile ToFile()
    {
        var joypads = new List<StoredJoypadProfile>(_joypads.Values);
        joypads.Sort((first, second) =>
        {
            int byModel = string.CompareOrdinal(first.Device.Guid, second.Device.Guid);
            return byModel != 0 ? byModel : first.Device.Ordinal.CompareTo(second.Device.Ordinal);
        });
        return new ControlsFile(_keyboard, _joypadDefault, joypads, _kept);
    }
}
