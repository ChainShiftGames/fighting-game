using System.IO;
using FightingGame.Input;
using FightingGame.Input.Profiles;
using Xunit;

namespace FightingGame.Tests.Input.Profiles;

public sealed class BuiltInPresetsTests
{
    [Fact]
    public void EveryShippedLayoutBindsEveryInputAndBothMenuButtons()
    {
        foreach (ControlPreset preset in TestButtons.Presets.All)
        {
            foreach (string input in TestButtons.Targets.Names())
            {
                Assert.True(preset.Profile.ControlsOf(BindingLayer.Game, input).Count > 0, $"{preset.Id}: {input} is unbound");
            }

            foreach (string input in MenuInputs.All)
            {
                Assert.True(preset.Profile.ControlsOf(BindingLayer.Menu, input).Count > 0, $"{preset.Id}: menu {input} is unbound");
            }
        }
    }

    [Fact]
    public void EachKindHasItsDefaultFirst()
    {
        Assert.Equal("keyboard_default", TestButtons.Presets.All[0].Id);
        Assert.Equal(InputDeviceKind.Joypad, TestButtons.Presets.DefaultOf(InputDeviceKind.Joypad).Kind);
        Assert.True(TestButtons.Presets.DefaultOf(InputDeviceKind.Joypad).Stick.LeftStick);
    }

    // Leverless boards in pad mode send the d-pad; their stick is off so a board in stick mode doesn't double up.
    [Fact]
    public void ThePadLeverlessPresetBindsTheDpadWithoutTheStick()
    {
        ControlPreset leverless = TestButtons.Presets.All[^1];

        Assert.Equal("pad_leverless", leverless.Id);
        Assert.False(leverless.Profile.Stick.LeftStick);
        Assert.Equal([BoundControl.OfButton(Godot.JoyButton.DpadLeft)], leverless.Profile.ControlsOf(BindingLayer.Game, "left"));
    }

    [Fact]
    public void AProblemInTheShippedLayoutsFailsLoudly()
    {
        string broken = """{ "presets": [ { "id": "k", "name": "N", "kind": "keyboard", "profile": { "bindings": { "a": ["Kp"] } } } ] }""";

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => BuiltInPresets.Parse(broken, TestButtons.Targets));
        Assert.Contains("'Kp'", exception.Message.Replace("\"", "'", System.StringComparison.Ordinal), System.StringComparison.Ordinal);
        Assert.Contains("no Joypad layout", exception.Message, System.StringComparison.Ordinal);
    }
}
