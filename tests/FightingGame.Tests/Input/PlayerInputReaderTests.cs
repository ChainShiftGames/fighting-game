using FightingGame.Input;
using FightingGame.Input.Profiles;
using Godot;
using Sim.Input;
using Xunit;
using static FightingGame.Tests.Input.TestJoypads;

namespace FightingGame.Tests.Input;

public sealed class PlayerInputReaderTests
{
    private readonly FakeDeviceState _devices = new();
    private readonly DeviceRoster _roster = new();
    private readonly ControlProfiles _profiles = new(TestButtons.Presets, ControlsFile.Empty);
    private readonly InputDeviceReader _reader;

    public PlayerInputReaderTests()
    {
        _reader = new InputDeviceReader(_profiles, TestButtons.Compiler, _devices, _roster);
        _roster.Update([Pad(0), Pad(1)]);
    }

    [Fact]
    public void TheDefaultGivesPlayer1TheKeyboardAndEachPlayerAPad()
    {
        PlayerInputReader[] players = PlayerInputReader.All(DefaultPlayerDevices.Versus(_roster), _reader);
        _devices.Hold(Key.A).Hold(1, JoyButton.DpadRight);

        Assert.Equal(InputWord.Left, players[0].Read().Bits);
        Assert.Equal(InputWord.Right, players[1].Read().Bits);
    }

    [Fact]
    public void ATestAssignmentDrivesTheRightPlayers()
    {
        var assignment = new PlayerDevices(2);
        assignment.Assign(0, Id(_roster, 1));
        assignment.Assign(1, InputDeviceId.Keyboard);
        PlayerInputReader[] players = PlayerInputReader.All(assignment, _reader);

        _devices.Hold(1, JoyButton.DpadLeft).Hold(0, JoyButton.DpadUp).Hold(Key.D);

        Assert.Equal(InputWord.Left, players[0].Read().Bits);
        Assert.Equal(InputWord.Right, players[1].Read().Bits);
    }

    // One keyboard, one layout: player 2 on the keyboard reads the same keys as player 1 would.
    [Fact]
    public void TheKeyboardKeepsItsLayoutOnEitherSide()
    {
        var assignment = new PlayerDevices(2);
        assignment.Assign(1, InputDeviceId.Keyboard);
        PlayerInputReader player2 = PlayerInputReader.All(assignment, _reader)[1];

        _devices.Hold(Key.Up);
        Assert.Equal(InputWord.Neutral, player2.Read());

        _devices.Hold(Key.W);
        Assert.Equal(InputWord.Up, player2.Read().Bits);
    }

    [Fact]
    public void APadsOwnLayoutFollowsItToTheOtherSide()
    {
        InputDeviceInfo pad = Device(1);
        _profiles.Set(pad, Remapped(pad, "a", JoyButton.Back));
        _devices.Hold(1, JoyButton.Back);
        ushort a = TestButtons.Repository.Mask("a");

        var onPlayer2 = new PlayerDevices(2);
        onPlayer2.Assign(1, pad.Id);
        Assert.Equal(a, PlayerInputReader.All(onPlayer2, _reader)[1].Read().Bits);

        var onPlayer1 = new PlayerDevices(2);
        onPlayer1.Assign(0, pad.Id);
        Assert.Equal(a, PlayerInputReader.All(onPlayer1, _reader)[0].Read().Bits);
    }

    [Fact]
    public void AnotherPadOfTheSameModelKeepsTheDefault()
    {
        _profiles.Set(Device(1), Remapped(Device(1), "a", JoyButton.Back));
        var assignment = new PlayerDevices(1);
        assignment.Assign(0, Device(0).Id);
        PlayerInputReader player = PlayerInputReader.All(assignment, _reader)[0];

        _devices.Hold(0, JoyButton.Back);

        Assert.Equal(InputWord.Neutral, player.Read());
    }

    // Layouts are compiled when the match starts: nothing is looked up or built per frame.
    [Fact]
    public void ALayoutChangedDuringAMatchCountsFromTheNextMatch()
    {
        PlayerDevices assignment = DefaultPlayerDevices.Versus(_roster);
        PlayerInputReader player2 = PlayerInputReader.All(assignment, _reader)[1];
        _profiles.Set(Device(1), Remapped(Device(1), "a", JoyButton.Back));
        _devices.Hold(1, JoyButton.Back);

        Assert.Equal(InputWord.Neutral, player2.Read());
        Assert.Equal(TestButtons.Repository.Mask("a"), PlayerInputReader.All(assignment, _reader)[1].Read().Bits);
    }

    [Fact]
    public void ADisconnectedPadReadsNeutral()
    {
        PlayerDevices assignment = DefaultPlayerDevices.Versus(_roster);
        PlayerInputReader player2 = PlayerInputReader.All(assignment, _reader)[1];
        _devices.Hold(1, JoyButton.DpadRight);

        _roster.Update([Pad(0)]);

        Assert.Equal(InputWord.Neutral, player2.Read());
    }

    private InputDeviceInfo Device(int joypad)
    {
        Assert.True(_roster.TryFindJoypad(joypad, out InputDeviceInfo device));
        return device;
    }

    private BindingProfile Remapped(InputDeviceInfo device, string input, JoyButton button) =>
        _profiles.For(device.Id).Bind(BindingLayer.Game, input, BoundControl.OfButton(button), replace: true);
}
