using FightingGame.Input;
using FightingGame.UI.SideSelect;
using Xunit;

namespace FightingGame.Tests.UI.SideSelect;

public sealed class DeviceIconsTests
{
    [Theory]
    [InlineData("PS5 Controller", DeviceIcon.Gamepad)]
    [InlineData("Xbox Series Controller", DeviceIcon.Gamepad)]
    [InlineData("Hit Box", DeviceIcon.ArcadeStick)]
    [InlineData("Snack Box Micro", DeviceIcon.ArcadeStick)]
    [InlineData("Qanba Arcade Joystick", DeviceIcon.ArcadeStick)]
    [InlineData("Victrix Pro FS Fight Stick", DeviceIcon.ArcadeStick)]
    public void AJoypadsNameTellsASticksFromAPad(string name, DeviceIcon icon) =>
        Assert.Equal(icon, DeviceIcons.Of(new InputDeviceInfo(InputDeviceId.Joypad("guid", 0), name, 0)));

    [Fact]
    public void TheKeyboardIsAKeyboard() =>
        Assert.Equal(
            DeviceIcon.Keyboard,
            DeviceIcons.Of(new InputDeviceInfo(InputDeviceId.Keyboard, "Keyboard", InputDeviceInfo.NoJoypad)));
}
