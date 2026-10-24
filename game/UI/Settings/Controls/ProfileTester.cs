using FightingGame.Input;
using FightingGame.Input.Profiles;
using Sim.Input;

namespace FightingGame.UI.Settings.Controls;

/// <summary>
/// The controls screen's test area: what the edited device gives with the layout as it is now, raw and after the
/// logic's own cleaning step, so a player sees the fixed SOCD rule (left + right = neutral, up + down = up) at work.
/// </summary>
public sealed class ProfileTester
{
    private readonly ProfileCompiler _compiler;
    private readonly IDeviceState _devices;
    private readonly DeviceRoster _roster;
    private readonly ButtonSet? _buttons;
    private readonly InputStep? _step;
    private BindingProfile? _compiledFor;
    private KeyboardDevice? _keyboard;
    private GamepadDevice? _gamepad;

    public ProfileTester(ProfileCompiler compiler, IDeviceState devices, DeviceRoster roster, ButtonSet? buttons)
    {
        _compiler = compiler;
        _devices = devices;
        _roster = roster;
        _buttons = buttons;
        _step = buttons is null ? null : new InputStep(buttons);
    }

    /// <summary>The raw word <paramref name="device"/> gives with <paramref name="profile"/>; neutral while disconnected.</summary>
    public InputWord Read(InputDeviceId device, BindingProfile profile)
    {
        if (!ReferenceEquals(profile, _compiledFor))
        {
            _compiledFor = profile;
            _keyboard = profile.Kind == InputDeviceKind.Keyboard ? new KeyboardDevice(_compiler.Keyboard(profile), _devices) : null;
            _gamepad = profile.Kind == InputDeviceKind.Joypad ? new GamepadDevice(_compiler.Gamepad(profile), _devices) : null;
        }

        if (!device.IsJoypad)
        {
            return _keyboard?.Read() ?? InputWord.Neutral;
        }

        return _gamepad is not null && _roster.TryGetJoypad(device, out int joypad) ? _gamepad.Read(joypad) : InputWord.Neutral;
    }

    /// <summary>The logic's cleaning step, so the test area can't disagree with what a match uses.</summary>
    public InputWord Clean(InputWord raw) => _step?.Clean(raw) ?? SocdResolver.Resolve(raw);

    public string Describe(InputWord word) => InputWordText.Describe(word, _buttons);
}
