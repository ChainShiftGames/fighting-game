using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.Tests.UI.Flow;
using FightingGame.Tests.UI.Online;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using FightingGame.UI.Results;
using Netcode.Handshake;
using Platform.Steam.Friends;
using Platform.Steam.Lobbies;
using Xunit;

namespace FightingGame.Tests.UI.Results;

// The guest's PC (P2) after an online match: the rematch goes through the lobby.
public sealed class OnlineResultsTests
{
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
    private readonly ResultsScreen _screen;
    private HandshakeRefusal _refusal = HandshakeRefusal.None;

    public OnlineResultsTests()
    {
        _lobby = new LobbyFlow(_steam);
        _navigator = new MenuNavigator(_stack, new MenuGrace(0));
        _flow = new GameFlow(_matches, _screens.Contains);
        _screen = new ResultsScreen(
            _flow,
            new ResultsChoices(),
            new SessionScore(),
            () => new MatchResult(MatchOutcome.Player2Wins, 1, 2),
            _ => new AnyDevicePlayer(),
            () => true,
            () => string.Empty,
            _lobby,
            () => _refusal);
        _flow.PressOnTitle(MenuDevice.Keyboard);
        _flow.Choose(LaunchMode.Online);

        // Ready, both picks in, the match played and over: the lobby offers a rematch.
        _lobby.Update();
        _lobby.Ready();
        _steam.OpponentReadyFor = 1;
        _lobby.Update();
        _flow.Advance(_flow.Setup);
        _lobby.Pick("0/ryu");
        _steam.OpponentPick = "1:0/ken";
        _lobby.Update();
        Assert.NotNull(_lobby.TakeMatchToStart());
        _flow.Follow(MatchPhase.Playing);
        _lobby.MatchEnded();
        Assert.Equal(LobbyStage.Rematch, _lobby.Stage);
    }

    [Fact]
    public void RematchWaitsForTheOtherPc()
    {
        Open();
        Assert.Equal(1, _screen.SideOf(0));

        Press(MenuAction.Confirm);
        _lobby.Ready(); // What the launcher does for the rematch.
        Follow();

        Assert.Equal(1, _matches.Rematches);
        Assert.Equal(FlowStep.Results, _flow.Step);
        Assert.True(_screen.WantsRematch(1));
        Assert.False(_screen.WantsRematch(0));
        Assert.Equal("LOBBY_REMATCH_WAITING", _screen.LabelOf(ResultsEntry.Rematch).Value);
        Assert.Equal("ONLINE_SELECT_WAITING", _screen.Hint.Value);

        _steam.OpponentReadyFor = 2;
        _lobby.Update();

        Assert.True(_screen.WantsRematch(0));
    }

    [Fact]
    public void ARefusedMatchOffersNoRematch()
    {
        _refusal = HandshakeRefusal.GameVersion;

        Open();

        Assert.Equal([ResultsEntry.LeaveLobby, ResultsEntry.MainMenu], _screen.Choices.Entries);
        Assert.Contains(_screen.Body, line => line.Value.StartsWith("ONLINE_CHECK", System.StringComparison.Ordinal));
    }

    [Fact]
    public void LeavingTheLobbyGoesBackToTheOnlineScreensAndEndsTheSession()
    {
        Open();
        Assert.Equal(1, _screen.Score.Matches);

        Press(MenuAction.Down, MenuAction.Confirm);
        Follow();

        Assert.Equal(FlowStep.Online, _flow.Step);
        Assert.Equal(0, _screen.Score.Matches);
    }

    private void Open()
    {
        _flow.Follow(MatchPhase.Over);
        Follow();
    }

    private void Follow()
    {
        _screen.Follow(_flow.Step);
        _stack.Show(_flow.Step == FlowStep.Results ? [_screen] : []);
    }

    private void Press(params MenuAction[] actions)
    {
        foreach (MenuAction action in actions)
        {
            _navigator.Handle(new MenuPress(action, _pad), AfterGrace);
        }
    }
}
