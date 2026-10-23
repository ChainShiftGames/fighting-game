using System.Collections.Generic;
using System.IO;
using FightingGame.Online;
using FightingGame.Tests.UI.Flow;
using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using Xunit;
using static FightingGame.Tests.UI.CharacterSelect.TestCharacters;

namespace FightingGame.Tests.UI.CharacterSelect;

public sealed class CharacterSelectScreenTests
{
    private const long ReadyTime = 900;
    private const long AfterGrace = 1000;
    private static readonly MenuDevice _player1Pad = MenuDevice.Gamepad(0);
    private static readonly MenuDevice _player2Pad = MenuDevice.Gamepad(1);

    private readonly RecordingMatchStarter _matches = new();
    private readonly HashSet<FlowStep> _screens = [FlowStep.Title, FlowStep.MainMenu, FlowStep.CharacterSelect, FlowStep.Results];
    private readonly TestPlayers _players = new();
    private readonly MenuStack _stack = new();
    private readonly MenuNavigator _navigator;
    private readonly GameFlow _flow;
    private readonly CharacterSelectScreen _screen;
    private IReadOnlyList<SelectableCharacter> _characters = Three();

    public CharacterSelectScreenTests()
    {
        _navigator = new MenuNavigator(_stack, new MenuGrace(0));
        _flow = new GameFlow(_matches, _screens.Contains);
        _screen = new CharacterSelectScreen(
            _flow,
            new CharacterSelection(new CharacterSelectSettings(Columns: 6, ReadyTime), max => max - 1),
            new PickMemory(),
            _ => _players,
            () => _characters);
        _flow.PressOnTitle(MenuDevice.Keyboard);
    }

    [Fact]
    public void BothPadsPickAndTheMatchStartsWithTheirCharactersAndColours()
    {
        Open(LaunchMode.Versus);

        Press(_player1Pad, MenuAction.Confirm, MenuAction.Right, MenuAction.Confirm);
        Press(_player2Pad, MenuAction.Right, MenuAction.Confirm, MenuAction.Confirm);
        _screen.Follow(isShown: true, 0);
        Assert.Empty(_matches.Started);
        _screen.Follow(isShown: true, ReadyTime);

        Assert.Equal(FlowStep.Match, _flow.Step);
        Assert.Equal(
            [new MatchSetup(LaunchMode.Versus, new FighterPick("ryu", 1), new FighterPick("ken", 0))],
            _matches.Started);
    }

    [Fact]
    public void BackOnTheGridLeavesTheScreen()
    {
        Open(LaunchMode.Versus);

        Press(_player2Pad, MenuAction.Back);

        Assert.Equal(FlowStep.MainMenu, _flow.Step);
    }

    [Fact]
    public void PressesFromNobodysDeviceAreIgnored()
    {
        Open(LaunchMode.Versus);

        Press(MenuDevice.Mouse, MenuAction.Confirm);

        Assert.Equal(SelectStep.Character, _screen.Selection.Side(0).Step);
    }

    [Fact]
    public void TrainingPicksForPlayerOneAndTheDummyMirrors()
    {
        _players.CanPressPlayer2 = false;
        Open(LaunchMode.Training);

        Press(_player1Pad, MenuAction.Confirm, MenuAction.Confirm);
        Finish();

        Assert.Equal(
            [new MatchSetup(LaunchMode.Training, new FighterPick("ryu", 0), new FighterPick("ryu", 1))],
            _matches.Started);
    }

    [Fact]
    public void CharacterSelectAfterAMatchStartsOnTheLastPicks()
    {
        Open(LaunchMode.Versus);
        Press(_player1Pad, MenuAction.Left, MenuAction.Left, MenuAction.Confirm, MenuAction.Right, MenuAction.Confirm);
        Press(_player2Pad, MenuAction.Confirm, MenuAction.Confirm);
        Finish();
        _flow.Follow(MatchPhase.Over);

        _flow.Reselect();
        _screen.Follow(isShown: true, 0);

        Assert.Equal(2, _screen.Selection.Side(0).Slot);
        Assert.Equal((2, 1), _screen.Selection.Shown(0));
    }

    [Fact]
    public void DataThatDoesntLoadIsShownAndOnlyBackWorks()
    {

        var screen = new CharacterSelectScreen(
            _flow,
            new CharacterSelection(CharacterSelectSettings.Default, _ => 0),
            new PickMemory(),
            _ => _players,
            () => throw new InvalidDataException("No characters: bad"));
        _flow.Choose(LaunchMode.Versus);
        screen.Follow(isShown: true, 0);

        Assert.Contains("bad", screen.Body[0].Value);
        screen.Press(new MenuPress(MenuAction.Confirm, _player1Pad));
        screen.Follow(isShown: true, ReadyTime);
        Assert.Equal(FlowStep.CharacterSelect, _flow.Step);
    }

    private void Open(LaunchMode mode)
    {
        _flow.Choose(mode);
        Assert.Equal(FlowStep.CharacterSelect, _flow.Step);
        _stack.Show([_screen]);
        _screen.Follow(isShown: true, 0);
    }

    private void Finish()
    {
        _screen.Follow(isShown: true, 0);
        _screen.Follow(isShown: true, ReadyTime);
        _screen.Follow(isShown: _flow.Step == FlowStep.CharacterSelect, ReadyTime);
    }

    // Through the navigator, as the menus hand presses on.
    private void Press(MenuDevice device, params MenuAction[] actions)
    {
        foreach (MenuAction action in actions)
        {
            _navigator.Handle(new MenuPress(action, device), AfterGrace);
        }
    }

    private sealed class TestPlayers : IMenuPlayers
    {
        public bool CanPressPlayer2 { get; set; } = true;

        public int PlayerOf(MenuDevice device) => device.Kind == MenuDeviceKind.Gamepad ? device.Index : -1;

        public bool CanPress(int player) => player == 0 || CanPressPlayer2;
    }
}
