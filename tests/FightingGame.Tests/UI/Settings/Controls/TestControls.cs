using FightingGame.Input;
using FightingGame.Input.Profiles;
using FightingGame.Tests.Input;
using FightingGame.UI.Settings.Controls;

namespace FightingGame.Tests.UI.Settings.Controls;

/// <summary>A controls menu over fake devices: the keyboard and two pads, the shipped layouts, a test clock.</summary>
internal sealed class TestControls
{
    public const long CaptureMilliseconds = 6000;

    public TestControls()
    {
        Roster.Update([TestJoypads.Pad(0), TestJoypads.Pad(1, TestJoypads.Hitbox)]);
        Menu = new ControlsMenu(
            Profiles,
            TestButtons.Targets.Names(),
            new ProfileTester(TestButtons.Compiler, Devices, Roster, TestButtons.Repository),
            new ControlsMenuTiming(CaptureMilliseconds),
            () => Now,
            () => Saves++);
    }

    public FakeDeviceState Devices { get; } = new();

    public DeviceRoster Roster { get; } = new();

    public ControlProfiles Profiles { get; } = new(TestButtons.Presets, ControlsFile.Empty);

    public ControlsMenu Menu { get; }

    public long Now { get; set; } = 1000;

    public int Saves { get; private set; }

    public InputDeviceInfo Keyboard => Roster.Devices[0];

    public InputDeviceInfo Pad => Roster.Devices[1];

    public InputDeviceInfo Hitbox => Roster.Devices[2];
}
