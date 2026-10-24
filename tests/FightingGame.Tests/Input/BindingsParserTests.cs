using System;
using FightingGame.Input;
using Godot;
using Sim.Input;
using Xunit;

namespace FightingGame.Tests.Input;

public sealed class BindingsParserTests
{
    private const string NoBindings = "{}";

    [Fact]
    public void TheOldDefaultsBindEveryButtonForBothPlayersOnBothDevices()
    {
        ButtonSet buttons = TestButtons.Repository;
        foreach (PlayerBindings player in TestButtons.OldDefaults())
        {
            for (int button = 0; button < buttons.Count; button++)
            {
                ushort mask = buttons.MaskOf(button);
                Assert.Contains(player.Keyboard.Keys, binding => binding.Mask == mask);
                Assert.True(
                    Array.Exists(player.Gamepad.Buttons, binding => binding.Mask == mask)
                    || Array.Exists(player.Gamepad.Triggers, binding => binding.Mask == mask),
                    $"gamepad button '{buttons.IdOf(button)}' is unbound");
            }
        }
    }

    [Fact]
    public void RebindingAKeyInTheFileChangesWhichKeyPressesTheButton()
    {
        PlayerBindings[] bindings = Parse(TwoPlayers("""{ "a": ["Q"] }""", NoBindings));
        FakeDeviceState devices = new FakeDeviceState().Hold(Key.Q);

        InputWord raw = new KeyboardDevice(bindings[0].Keyboard, devices).Read();

        Assert.Equal(TestButtons.Repository.Mask("a"), raw.Bits);
    }

    [Fact]
    public void RebindingAJoypadButtonInTheFileChangesWhichButtonPressesIt()
    {
        PlayerBindings[] bindings =
            Parse(TwoPlayers(NoBindings, """{ "dash": ["LeftShoulder"], "c": ["TriggerLeft"] }"""));
        FakeDeviceState devices = new FakeDeviceState()
            .Hold(0, JoyButton.LeftShoulder)
            .Move(0, JoyAxis.TriggerLeft, 1);

        InputWord raw = new GamepadDevice(bindings[0].Gamepad, devices).Read(0);

        ushort expected = (ushort)(TestButtons.Repository.Mask("dash") | TestButtons.Repository.Mask("c"));
        Assert.Equal(expected, raw.Bits);
    }

    [Fact]
    public void OneInputMayHaveSeveralControls()
    {
        PlayerBindings[] bindings = Parse(TwoPlayers("""{ "up": ["W", "Space"] }""", NoBindings));

        Assert.Equal(
            [new KeyBinding(Key.W, InputWord.Up), new KeyBinding(Key.Space, InputWord.Up)],
            bindings[0].Keyboard.Keys);
    }

    [Fact]
    public void ButtonNamesComeFromTheButtonSet()
    {
        var parser = new BindingsParser(TestButtons.FiveButtons, playerCount: 2);

        PlayerBindings[]? bindings = parser.Parse(TwoPlayers("""{ "dust": ["P"] }""", NoBindings));

        Assert.Empty(parser.Errors);
        Assert.Equal([new KeyBinding(Key.P, TestButtons.FiveButtons.Mask("dust"))], bindings![0].Keyboard.Keys);
    }

    [Fact]
    public void WithoutAButtonSetOnlyDirectionsAreBound()
    {
        var parser = new BindingsParser(buttons: null, playerCount: 2);

        PlayerBindings[]? bindings = parser.Parse(OldDevBindings.Defaults);

        Assert.Empty(parser.Errors);
        Assert.Equal(4, bindings![0].Keyboard.Keys.Length);
        Assert.All(bindings[0].Keyboard.Keys, binding => Assert.Equal(0, binding.Mask & ~InputWord.DirectionMask));
    }

    [Theory]
    [InlineData("""{ "a": ["Kp"] }""", "players[0].keyboard.bindings.a[0]: 'Kp' is not a Godot key name")]
    [InlineData("""{ "a": ["87"] }""", "'87' is not a Godot key name")]
    [InlineData("""{ "a": ["W, S"] }""", "'W, S' is not a Godot key name")]
    [InlineData("""{ "a": ["None"] }""", "'None' is not a Godot key name")]
    [InlineData("""{ "punch": ["W"] }""", "players[0].keyboard.bindings.punch: 'punch' is not an input")]
    [InlineData("""{ "a": "W" }""", "players[0].keyboard.bindings.a: must be a list of names")]
    public void ABadKeyboardBindingIsReportedWithItsPlace(string keyboard, string error)
    {
        AssertRejected(TwoPlayers(keyboard, NoBindings), error);
    }

    [Theory]
    [InlineData("""{ "a": ["Square"] }""", "players[0].gamepad.bindings.a[0]: 'Square' is not a Godot joypad")]
    [InlineData("""{ "a": ["LeftX"] }""", "'LeftX' is not a Godot joypad button or trigger")]
    [InlineData("""{ "a": ["Invalid"] }""", "'Invalid' is not a Godot joypad button or trigger")]
    public void ABadJoypadBindingIsReportedWithItsPlace(string gamepad, string error)
    {
        AssertRejected(TwoPlayers(NoBindings, gamepad), error);
    }

    [Fact]
    public void AMisspeltFieldIsReported()
    {
        string json = ReplaceOnce(TwoPlayers(NoBindings, NoBindings), "\"leftStick\"", "\"leftstick\"");

        AssertRejected(json, "players[0].gamepad.leftStick: is missing");
        AssertRejected(json, "players[0].gamepad.leftstick: is not a known field");
    }

    [Fact]
    public void ADeadzoneOutsideZeroToOneIsReported()
    {
        AssertRejected(
            ReplaceOnce(TwoPlayers(NoBindings, NoBindings), "\"stickDeadzone\": 0.5", "\"stickDeadzone\": 1.5"),
            "players[0].gamepad.stickDeadzone: must be a number from 0 to 1");
    }

    [Fact]
    public void TheFileMustBindEveryPlayer()
    {
        var parser = new BindingsParser(TestButtons.Repository, playerCount: 3);

        Assert.Null(parser.Parse(TwoPlayers(NoBindings, NoBindings)));
        Assert.Contains("file.players: lists 2 players; it needs exactly 3", parser.Errors);
    }

    [Fact]
    public void AnotherSchemaVersionIsReported()
    {
        AssertRejected(
            ReplaceOnce(TwoPlayers(NoBindings, NoBindings), "\"schemaVersion\": 1", "\"schemaVersion\": 2"),
            "file.schemaVersion: is 2, this game reads version 1");
    }

    [Fact]
    public void BrokenJsonIsReported()
    {
        AssertRejected("{ \"players\": [", "not valid JSON");
    }

    private static PlayerBindings[] Parse(string json)
    {
        var parser = new BindingsParser(TestButtons.Repository, playerCount: 2);
        PlayerBindings[]? bindings = parser.Parse(json);
        Assert.Empty(parser.Errors);
        return bindings!;
    }

    private static void AssertRejected(string json, string error)
    {
        var parser = new BindingsParser(TestButtons.Repository, playerCount: 2);

        Assert.Null(parser.Parse(json));
        Assert.Contains(parser.Errors, reported => reported.Contains(error, StringComparison.Ordinal));
    }

    private static string ReplaceOnce(string text, string oldValue, string newValue)
    {
        int index = text.IndexOf(oldValue, StringComparison.Ordinal);
        Assert.True(index >= 0, $"'{oldValue}' not found");
        return string.Concat(text.AsSpan(0, index), newValue, text.AsSpan(index + oldValue.Length));
    }

    // Player 1 gets the given bindings, player 2 none.
    private static string TwoPlayers(string keyboardBindings, string gamepadBindings) => $$"""
        // Comments and trailing commas are allowed: people edit this file by hand.
        {
          "schemaVersion": 1,
          "players": [
            {{Player(keyboardBindings, gamepadBindings)}},
            {{Player(NoBindings, NoBindings)}},
          ]
        }
        """;

    private static string Player(string keyboardBindings, string gamepadBindings) => $$"""
        {
          "keyboard": { "bindings": {{keyboardBindings}} },
          "gamepad": {
            "leftStick": true, "stickDeadzone": 0.5, "triggerThreshold": 0.5, "bindings": {{gamepadBindings}}
          }
        }
        """;
}
