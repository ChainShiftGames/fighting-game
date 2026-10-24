using System.Collections.Generic;
using System.Linq;
using FightingGame.Input;
using Xunit;
using static FightingGame.Tests.Input.TestJoypads;

namespace FightingGame.Tests.Input;

public sealed class DeviceRosterTests
{
    private readonly DeviceRoster _roster = new();
    private readonly List<string> _events = [];

    public DeviceRosterTests()
    {
        _roster.Connected += device => _events.Add($"+{device.Name}");
        _roster.Disconnected += device => _events.Add($"-{device.Name}");
    }

    [Fact]
    public void TheKeyboardIsAlwaysListedFirst()
    {
        _roster.Update([Pad(0)]);

        Assert.Equal(["Keyboard", "DualSense"], _roster.Devices.Select(device => device.Name));
    }

    // Steam Input re-presents a physical controller as a virtual pad: only one of the two may be listed.
    [Fact]
    public void APhysicalControllerSteamInputRepresentsIsListedOnce()
    {
        _roster.Update([new JoypadInfo(0, "DualSense", false, IgnoredBySteam: true, DualSense), Pad(1)]);

        Assert.Single(_roster.Devices, device => device.Id.IsJoypad);
        Assert.False(_roster.TryFindJoypad(0, out _));
    }

    [Fact]
    public void AReplugGivesThePadItsIdBack()
    {
        _roster.Update([Pad(0)]);
        InputDeviceId before = Id(_roster, 0);

        _roster.Update([]);
        Assert.False(_roster.IsConnected(before));
        _roster.Update([Pad(0)]);

        Assert.Equal(before, Id(_roster, 0));
    }

    [Fact]
    public void TwoPadsOfOneModelKeepTheirOwnIdsWhenOneIsUnplugged()
    {
        _roster.Update([Pad(0), Pad(1)]);
        InputDeviceId first = Id(_roster, 0);
        InputDeviceId second = Id(_roster, 1);
        Assert.NotEqual(first, second);

        _roster.Update([Pad(1)]);
        Assert.Equal(second, Id(_roster, 1));

        // Godot gives the replugged pad the free index again.
        _roster.Update([Pad(0), Pad(1)]);
        Assert.Equal(first, Id(_roster, 0));
        Assert.Equal(second, Id(_roster, 1));
    }

    [Fact]
    public void AnotherModelOnAFreedIndexIsANewDevice()
    {
        _roster.Update([Pad(0)]);
        InputDeviceId pad = Id(_roster, 0);

        _roster.Update([Pad(0, Hitbox)]);

        Assert.NotEqual(pad, Id(_roster, 0));
        Assert.Equal(["+DualSense", "-DualSense", "+Hitbox"], _events);
    }

    [Fact]
    public void OnlyChangesRaiseEvents()
    {
        _roster.Update([Pad(0)]);
        _roster.Update([Pad(0), Pad(1, Hitbox)]);
        _roster.Update([Pad(1, Hitbox)]);

        Assert.Equal(["+DualSense", "+Hitbox", "-DualSense"], _events);
    }

    [Fact]
    public void TheKeyboardHasNoJoypad()
    {
        Assert.False(_roster.TryGetJoypad(InputDeviceId.Keyboard, out _));
        Assert.True(_roster.IsConnected(InputDeviceId.Keyboard));
    }
}
