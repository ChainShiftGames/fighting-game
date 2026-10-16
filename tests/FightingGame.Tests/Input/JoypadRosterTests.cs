using FightingGame.Input;
using Xunit;

namespace FightingGame.Tests.Input;

public sealed class JoypadRosterTests
{
    private readonly JoypadRoster _roster = new();

    [Fact]
    public void SlotsFollowGodotsJoypadOrder()
    {
        _roster.Update([Raw(4), Raw(1)]);

        Assert.Equal(1, Device(0));
        Assert.Equal(4, Device(1));
    }

    // Steam Input re-presents a physical controller as a virtual pad; reading both would double every input.
    [Fact]
    public void APhysicalControllerSteamInputRepresentsIsSkipped()
    {
        _roster.Update([new JoypadInfo(0, "pad", false, IgnoredBySteam: true), Steam(1)]);

        Assert.Equal(1, Device(0));
        Assert.False(_roster.TryGetDevice(1, out _));
        Assert.Equal(-1, _roster.SlotOf(0));
    }

    // A controller Steam Input doesn't handle (e.g. a leverless with generic support off) still plays.
    [Fact]
    public void SteamInputAndRawJoypadsBothFillSlots()
    {
        _roster.Update([Steam(0), Raw(1)]);

        Assert.Equal(0, Device(0));
        Assert.Equal(1, Device(1));
    }

    [Fact]
    public void OneJoypadNeverFillsTwoSlots()
    {
        _roster.Update([Raw(2), Raw(2)]);

        Assert.Equal(2, Device(0));
        Assert.False(_roster.TryGetDevice(1, out _));
    }

    [Fact]
    public void UpdatingReplacesTheOldJoypads()
    {
        _roster.Update([Raw(0), Raw(1)]);
        _roster.Update([Raw(1)]);

        Assert.Equal(1, Device(0));
        Assert.Single(_roster.Connected);
    }

    private int Device(int slot)
    {
        Assert.True(_roster.TryGetDevice(slot, out int device));
        return device;
    }

    private static JoypadInfo Raw(int device) => new(device, "pad", IsSteamInput: false, IgnoredBySteam: false);

    private static JoypadInfo Steam(int device) => new(device, "Steam Virtual Gamepad", true, false);
}
