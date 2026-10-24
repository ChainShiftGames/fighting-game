using System;
using FightingGame.Input;
using FightingGame.Input.Profiles;
using Godot;
using Xunit;

namespace FightingGame.Tests.Input.Profiles;

public sealed class DevBindingsMigrationTests
{
    [Fact]
    public void TheOldDefaultsBecomeTheShippedDefaultsWithNothingExtraKept()
    {
        ControlsFile file = Migrate(OldDevBindings.Defaults);

        Assert.True(file.Keyboard!.SameAs(TestButtons.Presets.DefaultOf(InputDeviceKind.Keyboard)));
        Assert.True(file.JoypadDefault!.SameAs(TestButtons.Presets.DefaultOf(InputDeviceKind.Joypad)));
        Assert.Empty(file.Joypads);

        // Player 2's arrows and numpad are the shipped "arrows" preset, and both old pad layouts were the same.
        Assert.Empty(file.Presets);
    }

    [Fact]
    public void Player1sChangedKeysAndPadBecomeTheKeyboardAndThePadDefault()
    {
        string old = ReplaceOnce(OldDevBindings.Defaults, "\"a\": [\"U\"]", "\"a\": [\"Q\", \"U\"]");
        old = ReplaceOnce(old, "\"stickDeadzone\": 0.5", "\"stickDeadzone\": 0.3");
        old = ReplaceOnce(old, "\"a\": [\"X\"]", "\"a\": [\"Back\"]");

        ControlsFile file = Migrate(old);

        Assert.Equal([BoundControl.OfKey(Key.Q), BoundControl.OfKey(Key.U)], file.Keyboard!.ControlsOf(BindingLayer.Game, "a"));
        Assert.Equal(0.3f, file.JoypadDefault!.Stick.StickDeadzone);
        Assert.Equal([BoundControl.OfButton(JoyButton.Back)], file.JoypadDefault.ControlsOf(BindingLayer.Game, "a"));
        Assert.Equal(TestButtons.Presets.DefaultOf(InputDeviceKind.Joypad).Menu, file.JoypadDefault.Menu);
    }

    [Fact]
    public void Player2sOwnLayoutsAreKeptAsPresets()
    {
        string old = ReplaceOnce(OldDevBindings.Defaults, "\"a\": [\"Kp4\"]", "\"a\": [\"Kp7\"]");
        int player2Pad = old.LastIndexOf("\"a\": [\"X\"]", StringComparison.Ordinal);
        old = string.Concat(old.AsSpan(0, player2Pad), "\"a\": [\"Y\"]", old.AsSpan(player2Pad + "\"a\": [\"X\"]".Length));

        ControlsFile file = Migrate(old);

        Assert.Equal([DevBindingsMigration.Player2KeyboardPresetId, DevBindingsMigration.Player2JoypadPresetId], [.. Ids(file)]);
        Assert.Equal([BoundControl.OfKey(Key.Kp7)], file.Presets[0].Profile.ControlsOf(BindingLayer.Game, "a"));
        Assert.Equal(InputDeviceKind.Joypad, file.Presets[1].Profile.Kind);
    }

    [Fact]
    public void TheMigratedFileSavesAndLoadsAsItIs()
    {
        ControlsFile file = Migrate(ReplaceOnce(OldDevBindings.Defaults, "\"a\": [\"Kp4\"]", "\"a\": [\"Kp7\"]"));

        ControlsFile read = ControlsFileJson.Read(
            ControlsFileJson.Write(file), TestButtons.Targets, TestButtons.Presets, out string warning, out _);

        Assert.Empty(warning);
        Assert.True(read.Presets[0].Profile.SameAs(file.Presets[0].Profile));
        Assert.True(read.Keyboard!.SameAs(file.Keyboard!));
    }

    private static ControlsFile Migrate(string old)
    {
        var parser = new BindingsParser(TestButtons.Repository, BindingsParser.PlayerCount);
        PlayerBindings[]? players = parser.Parse(old);
        Assert.True(players is not null, string.Join(System.Environment.NewLine, parser.Errors));
        return DevBindingsMigration.Migrate(players, TestButtons.Targets, TestButtons.Presets);
    }

    private static string[] Ids(ControlsFile file) =>
        [.. System.Array.ConvertAll([.. file.Presets], preset => preset.Id)];

    private static string ReplaceOnce(string text, string oldValue, string newValue)
    {
        int index = text.IndexOf(oldValue, StringComparison.Ordinal);
        Assert.True(index >= 0, $"'{oldValue}' not found");
        return string.Concat(text.AsSpan(0, index), newValue, text.AsSpan(index + oldValue.Length));
    }
}
