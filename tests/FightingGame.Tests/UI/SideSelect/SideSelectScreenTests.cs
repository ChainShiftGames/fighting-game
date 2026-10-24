using System.Collections.Generic;
using FightingGame.Input;
using FightingGame.Online;
using FightingGame.Tests.UI.Flow;
using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using FightingGame.UI.SideSelect;
using Xunit;
using static FightingGame.Tests.Input.TestJoypads;
using static FightingGame.Tests.UI.CharacterSelect.TestCharacters;

namespace FightingGame.Tests.UI.SideSelect;

public sealed class SideSelectScreenTests
{
    private const long ReadyTime = 400;
    private const long AfterGrace = 1000;
    private static readonly MenuDevice _pad0 = MenuDevice.Gamepad(0);
    private static readonly MenuDevice _pad1 = MenuDevice.Gamepad(1);

    private readonly RecordingMatchStarter _matches = new();
    private readonly HashSet<FlowStep> _screens =
        [FlowStep.Title, FlowStep.MainMenu, FlowStep.SideSelect, FlowStep.CharacterSelect, FlowStep.Results];

    private readonly DeviceRoster _roster = new();
    private readonly PlayerDevicesChoice _choice;
    private readonly MenuStack _stack = new();
    private readonly MenuNavigator _navigator;
    private readonly GameFlow _flow;
    private readonly SideSelectScreen _screen;
    private readonly CharacterSelectScreen _characterSelect;

    public SideSelectScreenTests()
    {
        _roster.Update([Pad(0), Pad(1, Hitbox)]);
        _choice = new PlayerDevicesChoice(_roster, []);
        _navigator = new MenuNavigator(_stack, new MenuGrace(0));
        _flow = new GameFlow(_matches, _screens.Contains);
        _screen = new SideSelectScreen(
            _flow, new DeviceSides(new SideSelectSettings(ReadyTime, SlideMilliseconds: 0, ShakeMilliseconds: 0)), _choice);
        _characterSelect = new CharacterSelectScreen(
            _flow,
            new CharacterSelection(new CharacterSelectSettings(Columns: 6, BothReadyMilliseconds: 0), max => max - 1),
            new PickMemory(),
            mode => new AssignedMenuPlayers(mode == LaunchMode.Training ? _choice.Training() : _choice.Versus(), _roster),
            Three);
        _flow.PressOnTitle(MenuDevice.Keyboard);
    }

    [Fact]
    public void VersusGoesThroughSideSelectBeforeCharacterSelect()
    {
        _flow.Choose(LaunchMode.Versus);

        Assert.Equal(FlowStep.SideSelect, _flow.Step);
    }

    [Fact]
    public void EachPadPlaysTheSideItWentTo()
    {
        Open(LaunchMode.Versus);
        Press(_pad0, MenuAction.Right, MenuAction.Confirm);
        Press(_pad1, MenuAction.Left, MenuAction.Confirm);

        Finish();

        Assert.Equal(FlowStep.CharacterSelect, _flow.Step);
        PlayerDevices chosen = _choice.Versus();
        Assert.Equal([Id(_roster, 1)], chosen.Of(0));
        Assert.Equal([Id(_roster, 0)], chosen.Of(1));
        Assert.Equal(-1, chosen.PlayerOf(InputDeviceId.Keyboard));
    }

    [Fact]
    public void CharacterSelectMovesTheCursorOfTheSideEachPadChose()
    {
        Open(LaunchMode.Versus);
        Press(_pad0, MenuAction.Right, MenuAction.Confirm);
        Press(_pad1, MenuAction.Left, MenuAction.Confirm);
        Finish();

        var players = new AssignedMenuPlayers(_choice.Versus(), _roster);

        Assert.Equal(1, players.PlayerOf(_pad0));
        Assert.Equal(0, players.PlayerOf(_pad1));
        Assert.Equal(-1, players.PlayerOf(MenuDevice.Keyboard));
    }

    [Fact]
    public void BackOnCharacterSelectLandsOnSideSelectWithTheSidesAsChosen()
    {
        Open(LaunchMode.Versus);
        Press(_pad0, MenuAction.Right, MenuAction.Confirm);
        Press(_pad1, MenuAction.Left, MenuAction.Confirm);
        Finish();
        _stack.Show([_characterSelect]);
        _characterSelect.Follow(isShown: true, 0);

        Press(_pad0, MenuAction.Back);
        _screen.Follow(isShown: _flow.Step == FlowStep.SideSelect, 0);

        Assert.Equal(FlowStep.SideSelect, _flow.Step);
        Assert.Equal(_roster.Devices[2].Id, _screen.Sides.On(0)!.Id);
        Assert.Equal(_roster.Devices[1].Id, _screen.Sides.On(1)!.Id);
    }

    [Fact]
    public void ANewVersusStartsWithTheSameDevicesOnTheSameSides()
    {
        Open(LaunchMode.Versus);
        Press(_pad0, MenuAction.Right, MenuAction.Confirm);
        Press(_pad1, MenuAction.Left, MenuAction.Confirm);
        Finish();
        _flow.ToMainMenu();
        _screen.Follow(isShown: false, 0);

        Open(LaunchMode.Versus);

        Assert.Equal(_roster.Devices[2].Id, _screen.Sides.On(0)!.Id);
        Assert.Equal(_roster.Devices[1].Id, _screen.Sides.On(1)!.Id);
        Press(_pad0, MenuAction.Confirm);
        Press(_pad1, MenuAction.Confirm);
        Finish();
        Assert.Equal(FlowStep.CharacterSelect, _flow.Step);
    }

    [Fact]
    public void BackOnADeviceInTheMiddleLeavesToTheMainMenu()
    {
        Open(LaunchMode.Versus);

        Press(MenuDevice.Keyboard, MenuAction.Back | MenuAction.Start);

        Assert.Equal(FlowStep.MainMenu, _flow.Step);
    }

    [Fact]
    public void TrainingContinuesWithPlayerOneAlone()
    {
        Open(LaunchMode.Training);

        Press(_pad1, MenuAction.Confirm);
        Finish();

        Assert.Equal(FlowStep.CharacterSelect, _flow.Step);
        PlayerDevices chosen = _choice.Training();
        Assert.Equal(1, chosen.PlayerCount);
        Assert.Equal([Id(_roster, 1)], chosen.Of(0));
        Assert.Null(_choice.ChosenVersus);
    }

    [Fact]
    public void TheQuickPathIsOneConfirm()
    {
        _roster.Update([]);
        Open(LaunchMode.Training);

        Press(MenuDevice.Keyboard, MenuAction.Confirm);
        Finish();

        Assert.Equal(FlowStep.CharacterSelect, _flow.Step);
        Assert.Equal([InputDeviceId.Keyboard], _choice.Training().Of(0));
    }

    [Fact]
    public void APadsStartReadiesItToo()
    {
        Open(LaunchMode.Training);

        Press(_pad0, MenuAction.Start);

        Assert.True(_screen.Sides.On(0)!.IsReady);
    }

    [Fact]
    public void HeldDirectionsDontSlideAcrossTheScreen()
    {
        Open(LaunchMode.Versus);
        Press(_pad0, MenuAction.Right);

        _navigator.Handle(new MenuPress(MenuAction.Left, _pad0, IsRepeat: true), AfterGrace);

        Assert.Equal(SideColumn.Player2, _screen.Sides.On(1)!.Column);
    }

    [Fact]
    public void ThePadsPluggedInOnTheScreenShowUp()
    {
        Open(LaunchMode.Versus);
        _roster.Update([Pad(0)]);
        _screen.Follow(isShown: true, 0);
        Assert.Equal(2, _screen.Sides.Devices.Count);

        _roster.Update([Pad(0), Pad(1, Hitbox)]);
        _screen.Follow(isShown: true, 0);

        Assert.Equal(3, _screen.Sides.Devices.Count);
    }

    [Fact]
    public void WithoutAChoiceMatchesKeepTheDefaultAssignment()
    {
        Assert.True(_choice.Versus().ClaimsNewJoypads);
        Assert.True(_choice.Training().ClaimsNewJoypads);
    }

    [Fact]
    public void TheChoiceIsKeptForTheMatchesThatFollow()
    {
        Open(LaunchMode.Versus);
        Press(_pad0, MenuAction.Confirm);
        Press(_pad1, MenuAction.Confirm);
        Finish();

        Assert.Same(_choice.Versus(), _choice.Versus());
        Assert.False(_choice.Versus().ClaimsNewJoypads);
    }

    private void Open(LaunchMode mode)
    {
        _flow.Choose(mode);
        Assert.Equal(FlowStep.SideSelect, _flow.Step);
        _stack.Show([_screen]);
        _screen.Follow(isShown: true, 0);
    }

    private void Finish()
    {
        _screen.Follow(isShown: true, 0);
        _screen.Follow(isShown: true, ReadyTime);
        _screen.Follow(isShown: _flow.Step == FlowStep.SideSelect, ReadyTime);
    }

    // Through the navigator, as the menus hand presses on.
    private void Press(MenuDevice device, params MenuAction[] actions)
    {
        foreach (MenuAction action in actions)
        {
            _navigator.Handle(new MenuPress(action, device), AfterGrace);
        }
    }
}
