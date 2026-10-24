using System;
using FightingGame.Input;
using FightingGame.Input.Profiles;
using Godot;
using Xunit;

namespace FightingGame.Tests.Input.Profiles;

public sealed class ControlProfilesTests
{
    private static readonly InputDeviceInfo _keyboard = new(InputDeviceId.Keyboard, "Keyboard", InputDeviceInfo.NoJoypad);
    private static readonly InputDeviceInfo _pad = new(InputDeviceId.Joypad("dualsense-guid", 0), "DualSense", 0);
    private static readonly InputDeviceInfo _secondPad = new(InputDeviceId.Joypad("dualsense-guid", 1), "DualSense", 1);

    private readonly ControlProfiles _profiles = new(TestButtons.Presets, ControlsFile.Empty);

    [Fact]
    public void ANewDeviceStartsFromItsKindsDefault()
    {
        Assert.Same(TestButtons.Presets.DefaultOf(InputDeviceKind.Keyboard), _profiles.For(_keyboard.Id));
        Assert.Same(TestButtons.Presets.DefaultOf(InputDeviceKind.Joypad), _profiles.For(_pad.Id));
    }

    [Fact]
    public void AMigratedPadDefaultIsWhatNewPadsStartFrom()
    {
        BindingProfile old = Remapped(TestButtons.Presets.DefaultOf(InputDeviceKind.Joypad));
        var profiles = new ControlProfiles(TestButtons.Presets, ControlsFile.Empty with { JoypadDefault = old });

        Assert.Same(old, profiles.For(_pad.Id));
    }

    [Fact]
    public void EachPadKeepsItsOwnLayout()
    {
        BindingProfile remapped = Remapped(_profiles.For(_pad.Id));

        _profiles.Set(_pad, remapped);

        Assert.Same(remapped, _profiles.For(_pad.Id));
        Assert.Same(TestButtons.Presets.DefaultOf(InputDeviceKind.Joypad), _profiles.For(_secondPad.Id));
        Assert.Equal(1, _profiles.Changes);
    }

    [Fact]
    public void AKeyboardLayoutCantGoOnAPad()
    {
        Assert.Throws<ArgumentException>(() => _profiles.Set(_pad, _profiles.For(_keyboard.Id)));
    }

    [Fact]
    public void ResetGoesBackToTheShippedDefault()
    {
        var profiles = new ControlProfiles(
            TestButtons.Presets, ControlsFile.Empty with { JoypadDefault = Remapped(TestButtons.Presets.DefaultOf(InputDeviceKind.Joypad)) });

        profiles.ResetToDefault(_pad);

        Assert.Same(TestButtons.Presets.DefaultOf(InputDeviceKind.Joypad), profiles.For(_pad.Id));
    }

    [Fact]
    public void SavedLayoutsComeBackAfterARestart()
    {
        _profiles.Set(_secondPad, Remapped(_profiles.For(_secondPad.Id)));
        _profiles.Set(_pad, Remapped(_profiles.For(_pad.Id)));
        _profiles.Set(_keyboard, _profiles.For(_keyboard.Id).Bind(BindingLayer.Game, "a", BoundControl.OfKey(Key.Q), replace: true));

        string saved = ControlsFileJson.Write(_profiles.ToFile());
        var restarted = new ControlProfiles(
            TestButtons.Presets, ControlsFileJson.Read(saved, TestButtons.Targets, TestButtons.Presets, out _, out _));

        Assert.True(restarted.For(_pad.Id).SameAs(_profiles.For(_pad.Id)));
        Assert.True(restarted.For(_keyboard.Id).SameAs(_profiles.For(_keyboard.Id)));
        Assert.Collection(
            restarted.ToFile().Joypads,
            joypad => Assert.Equal(_pad.Id, joypad.Device),
            joypad => Assert.Equal(_secondPad.Id, joypad.Device));
    }

    [Fact]
    public void PresetsAreOnlyThoseOfTheDevicesKind()
    {
        Assert.Equal(["keyboard_default", "keyboard_arrows", "keyboard_leverless"], [.. _profiles.PresetsFor(InputDeviceKind.Keyboard).ConvertAll(preset => preset.Id)]);
        Assert.Equal(["pad_classic", "pad_leverless"], [.. _profiles.PresetsFor(InputDeviceKind.Joypad).ConvertAll(preset => preset.Id)]);
    }

    private static BindingProfile Remapped(BindingProfile pad) =>
        pad.Bind(BindingLayer.Game, "a", BoundControl.OfButton(JoyButton.Back), replace: true);
}
