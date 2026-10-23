using FightingGame.Input;

namespace FightingGame.Tests.Input;

/// <summary>Joypads as Godot would report them, for the device roster tests.</summary>
internal static class TestJoypads
{
    public const string DualSense = "dualsense-guid";
    public const string Hitbox = "hitbox-guid";

    public static JoypadInfo Pad(int device, string guid = DualSense) =>
        new(device, guid == DualSense ? "DualSense" : "Hitbox", IsSteamInput: false, IgnoredBySteam: false, guid);

    public static InputDeviceId Id(DeviceRoster roster, int joypad)
    {
        Xunit.Assert.True(roster.TryFindJoypad(joypad, out InputDeviceInfo device), $"joypad {joypad} isn't in the roster");
        return device.Id;
    }
}
