using System;
using FightingGame.Input;
using FightingGame.Input.Profiles;
using Godot;
using Xunit;

namespace FightingGame.Tests.Input.Profiles;

public sealed class ControlsFileJsonTests
{
    private static readonly InputDeviceId _pad = InputDeviceId.Joypad("dualsense-guid", 1);

    [Fact]
    public void EveryPartSurvivesARoundTrip()
    {
        BindingProfile keyboard = TestButtons.Presets.DefaultOf(InputDeviceKind.Keyboard)
            .Bind(BindingLayer.Game, "a", BoundControl.OfKey(Key.Q), replace: true)
            .Bind(BindingLayer.Menu, MenuInputs.Confirm, BoundControl.OfKey(Key.J), replace: true);
        BindingProfile pad = TestButtons.Presets.DefaultOf(InputDeviceKind.Joypad)
            .Bind(BindingLayer.Game, "dash", BoundControl.OfTrigger(JoyAxis.TriggerLeft), replace: false)
            .WithStick(new StickOptions(false, 0.25f, 0.75f));
        var file = new ControlsFile(
            keyboard,
            pad,
            [new StoredJoypadProfile(_pad, "DualSense", pad)],
            [new ControlPreset("old_player2_keyboard", "MENU_CONTROLS_PRESET_OLD_P2_KEYBOARD", keyboard)]);

        ControlsFile read = Read(ControlsFileJson.Write(file), out string warning);

        Assert.Empty(warning);
        Assert.True(read.Keyboard!.SameAs(keyboard));
        Assert.True(read.JoypadDefault!.SameAs(pad));
        StoredJoypadProfile joypad = Assert.Single(read.Joypads);
        Assert.Equal((_pad, "DualSense"), (joypad.Device, joypad.Name));
        Assert.True(joypad.Profile.SameAs(pad));
        ControlPreset preset = Assert.Single(read.Presets);
        Assert.Equal(("old_player2_keyboard", "MENU_CONTROLS_PRESET_OLD_P2_KEYBOARD"), (preset.Id, preset.NameKey));
        Assert.True(preset.Profile.SameAs(keyboard));
    }

    [Fact]
    public void TheVersionIsWrittenFirst()
    {
        Assert.StartsWith("{\n  \"schemaVersion\": 1,", ControlsFileJson.Write(ControlsFile.Empty).Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownControlIsDroppedWithAWarningAndTheRestKept()
    {
        ControlsFile read = Read(
            """{ "schemaVersion": 1, "keyboard": { "bindings": { "a": ["Kp", "Q"], "punch": ["W"] } } }""", out string warning);

        Assert.Equal([BoundControl.OfKey(Key.Q)], read.Keyboard!.ControlsOf(BindingLayer.Game, "a"));
        Assert.Contains("keyboard.bindings.a[0]", warning, StringComparison.Ordinal);
        Assert.Contains("'punch' is not an input", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingPartComesFromTheDefault()
    {
        ControlsFile read = Read("""{ "schemaVersion": 1, "keyboard": { "bindings": { "a": ["Q"] } } }""", out _);

        BindingProfile defaults = TestButtons.Presets.DefaultOf(InputDeviceKind.Keyboard);
        Assert.Equal(defaults.Menu, read.Keyboard!.Menu);
    }

    [Fact]
    public void ATriggerCantBeAMenuButton()
    {
        ControlsFile read = Read(
            """{ "schemaVersion": 1, "joypadDefault": { "menu": { "confirm": ["TriggerRight", "A"] } } }""", out string warning);

        Assert.Equal([BoundControl.OfButton(JoyButton.A)], read.JoypadDefault!.ControlsOf(BindingLayer.Menu, MenuInputs.Confirm));
        Assert.Contains("joypadDefault.menu.confirm[0]", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void ADeadzoneOutsideZeroToOneKeepsTheDefault()
    {
        ControlsFile read = Read("""{ "schemaVersion": 1, "joypadDefault": { "stickDeadzone": 1.5 } }""", out string warning);

        Assert.Equal(0.5f, read.JoypadDefault!.Stick.StickDeadzone);
        Assert.Contains("joypadDefault.stickDeadzone: must be a number from 0 to 1", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void ANewerVersionIsReadByTheNamesThisGameKnows()
    {
        ControlsFile read = Read(
            """{ "schemaVersion": 9, "keyboard": { "bindings": { "a": ["Q"] }, "glow": true } }""", out string warning);

        Assert.Equal([BoundControl.OfKey(Key.Q)], read.Keyboard!.ControlsOf(BindingLayer.Game, "a"));
        Assert.Contains("schemaVersion 9 is newer than 1", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void BrokenJsonIsUnreadableAndGivesTheDefaults()
    {
        ControlsFile read = ControlsFileJson.Read(
            "{ \"keyboard\": ", TestButtons.Targets, TestButtons.Presets, out string warning, out bool isUnreadable);

        Assert.True(isUnreadable);
        Assert.Same(ControlsFile.Empty, read);
        Assert.Contains("not valid JSON", warning, StringComparison.Ordinal);
    }

    private static ControlsFile Read(string json, out string warning)
    {
        ControlsFile file = ControlsFileJson.Read(json, TestButtons.Targets, TestButtons.Presets, out warning, out bool isUnreadable);
        Assert.False(isUnreadable);
        return file;
    }
}
