using System.Collections.Generic;
using FightingGame.Input;
using FightingGame.Online;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;

namespace FightingGame.UI.SideSelect;

/// <summary>
/// Side select: every connected device slides to player 1, player 2 or the middle and readies there
/// (<see cref="DeviceSides"/>); the sides become the match's assignment (<see cref="PlayerDevicesChoice"/>), then on to
/// character select. It starts from the last assignment chosen this session. Drawn by <see cref="SideSelectView"/>.
/// </summary>
/// <remarks>
/// Versus and training only: online has one player on this PC, who plays the side the lobby gives them with any of
/// its devices, so there is nothing to choose.
/// </remarks>
public sealed class SideSelectScreen : IMenuScreen
{
    private readonly GameFlow _flow;
    private readonly DeviceSides _sides;
    private readonly PlayerDevicesChoice _choice;
    private bool _isOpen;

    public SideSelectScreen(GameFlow flow, DeviceSides sides, PlayerDevicesChoice choice)
    {
        _flow = flow;
        _sides = sides;
        _choice = choice;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_TITLE_SIDE_SELECT");

    public IReadOnlyList<MenuText> Body { get; } = [];

    public IReadOnlyList<MenuItem> Items { get; } = [];

    public bool IsOverMatch => false;

    /// <summary>Every device moves its own icon, so every press comes with its device.</summary>
    public bool TakesEveryPress => true;

    public DeviceSides Sides => _sides;

    private bool IsTraining => _flow.Setup.Mode == LaunchMode.Training;

    /// <summary>Opens and closes with the flow, follows the roster and moves on once all are ready; call once per displayed frame.</summary>
    public void Follow(bool isShown, long nowMilliseconds)
    {
        if (!isShown)
        {
            _isOpen = false;
            return;
        }

        if (!_isOpen)
        {
            _isOpen = true;
            PlayerDevices? remembered = IsTraining
                ? _choice.ChosenTraining ?? _choice.ChosenVersus
                : _choice.ChosenVersus ?? _choice.ChosenTraining;
            _sides.Begin(_choice.Roster.Devices, IsTraining ? SideRules.Training : SideRules.Versus, remembered);
        }

        _sides.Follow(_choice.Roster.Devices);
        if (_sides.Update(nowMilliseconds))
        {
            Finish();
        }
    }

    public void Press(MenuPress press)
    {
        // Holding a direction would slide an icon straight across the screen.
        if (press.IsRepeat || !TryFind(press.Device, out InputDeviceInfo device))
        {
            return;
        }

        // A pad's Start also readies; the keyboard's Escape is back and start at once, and stays back.
        MenuAction actions = press.Actions;
        if (press.Has(MenuAction.Start) && !press.Has(MenuAction.Back))
        {
            actions |= MenuAction.Confirm;
        }

        if (_sides.Press(device, actions))
        {
            _flow.Back();
        }
    }

    // Every press reaches Press with its device (TakesEveryPress); back never comes here.
    public bool Back() => false;

    private bool TryFind(MenuDevice menuDevice, out InputDeviceInfo device)
    {
        DeviceRoster roster = _choice.Roster;
        switch (menuDevice.Kind)
        {
            case MenuDeviceKind.Keyboard:
                device = roster.Devices[0];
                return !device.Id.IsJoypad;
            case MenuDeviceKind.Gamepad:
                return roster.TryFindJoypad(menuDevice.Index, out device);
            default:
                device = default;
                return false;
        }
    }

    private void Finish()
    {
        PlayerDevices assignment = _sides.ToAssignment();
        if (IsTraining)
        {
            _choice.ChooseTraining(assignment);
        }
        else
        {
            _choice.ChooseVersus(assignment);
        }

        _flow.Advance(_flow.Setup);
    }
}
