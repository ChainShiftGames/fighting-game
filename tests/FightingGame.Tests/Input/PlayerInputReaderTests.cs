using FightingGame.Input;
using Godot;
using Sim.Input;
using Xunit;
using static FightingGame.Tests.Input.TestJoypads;

namespace FightingGame.Tests.Input;

public sealed class PlayerInputReaderTests
{
    private readonly FakeDeviceState _devices = new();
    private readonly DeviceRoster _roster = new();
    private readonly InputDeviceReader _reader;

    public PlayerInputReaderTests()
    {
        _reader = new InputDeviceReader(TestButtons.Defaults(), _devices, _roster);
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

        _devices.Hold(1, JoyButton.DpadLeft).Hold(0, JoyButton.DpadUp).Hold(Key.Right);

        Assert.Equal(InputWord.Left, players[0].Read().Bits);
        Assert.Equal(InputWord.Right, players[1].Read().Bits);
    }

    // The keyboard reads with the bindings of whoever plays on it, like a pad.
    [Fact]
    public void TheKeyboardUsesThePlayersOwnKeys()
    {
        var assignment = new PlayerDevices(2);
        assignment.Assign(1, InputDeviceId.Keyboard);
        var player2 = new PlayerInputReader(1, assignment, _reader);

        _devices.Hold(Key.W);
        Assert.Equal(InputWord.Neutral, player2.Read());

        _devices.Hold(Key.Up);
        Assert.Equal(InputWord.Up, player2.Read().Bits);
    }

    [Fact]
    public void ADisconnectedPadReadsNeutral()
    {
        PlayerDevices assignment = DefaultPlayerDevices.Versus(_roster);
        _devices.Hold(1, JoyButton.DpadRight);

        _roster.Update([Pad(0)]);

        Assert.Equal(InputWord.Neutral, new PlayerInputReader(1, assignment, _reader).Read());
    }
}
