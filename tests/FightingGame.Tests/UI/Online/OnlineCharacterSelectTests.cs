using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.Tests.UI.Flow;
using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using FightingGame.UI.Online;
using Platform.Steam.Friends;
using Platform.Steam.Lobbies;
using Xunit;
using static FightingGame.Tests.UI.CharacterSelect.TestCharacters;

namespace FightingGame.Tests.UI.Online;

// The guest's PC: it plays P2, and the host's pick arrives through the lobby.
public sealed class OnlineCharacterSelectTests
{
    private const long ReadyTime = 900;
    private const long AfterGrace = 1000;
    private static readonly MenuDevice _pad = MenuDevice.Gamepad(0);

    private readonly RecordingMatchStarter _matches = new();
    private readonly HashSet<FlowStep> _screens =
        [FlowStep.Title, FlowStep.MainMenu, FlowStep.Online, FlowStep.CharacterSelect, FlowStep.Results];

    private readonly StubLobby _steam = new() { Presence = LobbyPresence.Inside, Opponent = new SteamFriend(42, "Host") };
    private readonly LobbyFlow _lobby;
    private readonly MenuStack _stack = new();
    private readonly MenuNavigator _navigator;
    private readonly GameFlow _flow;
    private readonly CharacterSelectScreen _screen;

    public OnlineCharacterSelectTests()
    {
        _lobby = new LobbyFlow(_steam);
        _navigator = new MenuNavigator(_stack, new MenuGrace(0));
        _flow = new GameFlow(_matches, _screens.Contains);
        var online = new LobbyPicking(_lobby);
        _screen = new CharacterSelectScreen(
            _flow,
            new CharacterSelection(new CharacterSelectSettings(Columns: 6, ReadyTime), max => max - 1),
            new PickMemory(),
            _ => new OnlineMenuPlayers(new OnePadPlayer(), online.LocalSide),
            Three,
            _ => online);
        _flow.PressOnTitle(MenuDevice.Keyboard);
        _flow.Choose(LaunchMode.Online);

        // Both ready in the lobby: on to character select.
        _lobby.Update();
        _lobby.Ready();
        _steam.OpponentReadyFor = 1;
        _lobby.Update();
        Assert.Equal(LobbyStage.Picking, _lobby.Stage);
        _flow.Advance(_flow.Setup);
        _stack.Show([_screen]);
        _screen.Follow(isShown: true, 0);
    }

    [Fact]
    public void ThisPcPicksForItsSideAndTheOtherSideWaits()
    {
        Assert.False(_screen.Selection.Chooses(0));
        Assert.True(_screen.Selection.IsRemote(0));
        Assert.True(_screen.Selection.Chooses(1));
        Assert.Equal((-1, 0), _screen.Selection.Shown(0));

        Press(MenuAction.Right, MenuAction.Confirm, MenuAction.Confirm);
        Finish();

        Assert.Equal("1:0/ken", _steam.LastPickMark);
        Assert.True(_screen.IsWaitingForOpponent);
        Assert.Equal(FlowStep.CharacterSelect, _flow.Step);
        Assert.Empty(_matches.Started);
    }

    [Fact]
    public void TheOtherPicksShowsWhenItArrivesAndBothPicksStartTheMatch()
    {
        Press(MenuAction.Confirm, MenuAction.Confirm);
        Finish();

        _steam.OpponentPick = "1:2/guile";
        _lobby.Update();
        _screen.Follow(isShown: true, ReadyTime);

        Assert.Equal((2, 2), _screen.Selection.Shown(0));
        Assert.False(_screen.IsWaitingForOpponent);
        Assert.Equal(LobbyStage.Starting, _lobby.Stage);
        LobbyMatch match = _lobby.TakeMatchToStart()!;
        Assert.Equal("0/ryu", match.LocalPick);
        Assert.Equal("2/guile", match.OpponentPick);
    }

    [Fact]
    public void TheSecondToLockInAMirrorGivesWayOnColour()
    {
        _steam.OpponentPick = "1:0/ryu";
        _lobby.Update();
        _screen.Follow(isShown: true, 0);

        Press(MenuAction.Confirm, MenuAction.Confirm);
        Finish();

        Assert.Equal("1:1/ryu", _steam.LastPickMark);
    }

    [Fact]
    public void ASentPickCantBeUndoneBackLeavesTheLobby()
    {
        Press(MenuAction.Confirm, MenuAction.Confirm);
        Finish();

        Press(MenuAction.Back);

        Assert.Equal(LobbyStage.Idle, _lobby.Stage);
        Assert.Equal(FlowStep.Online, _flow.Step);
    }

    [Fact]
    public void BackOnTheGridLeavesTheLobby()
    {
        Press(MenuAction.Back);

        Assert.Equal(LobbyStage.Idle, _lobby.Stage);
        Assert.Equal(FlowStep.Online, _flow.Step);
    }

    private void Finish()
    {
        _screen.Follow(isShown: true, 0);
        _screen.Follow(isShown: true, ReadyTime);
    }

    private void Press(params MenuAction[] actions)
    {
        foreach (MenuAction action in actions)
        {
            _navigator.Handle(new MenuPress(action, _pad), AfterGrace);
        }
    }

    // The online assignment: one player on this PC, on the first pad.
    private sealed class OnePadPlayer : IMenuPlayers
    {
        public int PlayerOf(MenuDevice device) => device == _pad ? 0 : -1;

        public bool CanPress(int player) => player == 0;
    }
}
