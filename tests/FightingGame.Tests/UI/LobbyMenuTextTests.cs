using System;
using System.Linq;
using FightingGame.Tests.UI.Framework;
using FightingGame.UI;
using FightingGame.UI.Framework;
using Platform.Steam.Friends;
using Platform.Steam.Lobbies;
using Xunit;

namespace FightingGame.Tests.UI;

public sealed class LobbyMenuTextTests
{
    private static readonly SteamFriend _rival = new(42, "Rival");

    [Fact]
    public void IdleOffersToHostOrJoinAFriendInALobby()
    {
        var lobby = new StubLobby();
        lobby.Friends.Add(_rival);
        lobby.Friends.Add(new SteamFriend(43, "Host", LobbyId: 900));
        using var flow = new LobbyFlow(lobby);
        flow.RefreshFriends();

        string text = Text(flow, MenuText.Empty);

        Assert.Contains("Host a lobby and invite a friend", text, StringComparison.Ordinal);
        Assert.Equal(["Host a lobby", "Join Host's lobby", "Back"], Labels(flow));
        Assert.Equal(1, LobbyMenuItems.For(flow)[1].FriendIndex);
    }

    [Fact]
    public void WaitingForAnOpponentListsInvites()
    {
        var lobby = new StubLobby();
        lobby.Friends.Add(_rival);
        using LobbyFlow flow = Hosting(lobby);
        flow.RefreshFriends();

        string text = Text(flow, MenuText.Key("LOBBY_INVITE_SENT", "Rival"));

        Assert.Equal(["Invite through the Steam overlay", "Invite Rival", "Leave the lobby"], Labels(flow));
        Assert.Contains("waiting for an opponent", text, StringComparison.Ordinal);
        Assert.EndsWith("Invite sent to Rival.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WithAnOpponentShowsPortsAndWhoIsReady()
    {
        var lobby = new StubLobby();
        using LobbyFlow flow = Hosting(lobby);
        lobby.Opponent = _rival;
        lobby.OpponentReadyFor = 1;
        flow.Update();

        string text = Text(flow, MenuText.Empty);

        Assert.Contains("you (P1) vs Rival (P2)", text, StringComparison.Ordinal);
        Assert.Contains("You: not yet", text, StringComparison.Ordinal);
        Assert.Contains("Rival: ready", text, StringComparison.Ordinal);
        Assert.Equal(["Ready", "Leave the lobby"], Labels(flow));
    }

    [Fact]
    public void ReadyCantBePressedTwice()
    {
        var lobby = new StubLobby();
        using LobbyFlow flow = Hosting(lobby);
        lobby.Opponent = _rival;
        flow.Update();
        flow.Ready();

        LobbyMenuItem ready = LobbyMenuItems.For(flow)[0];

        Assert.Equal(LobbyChoice.Ready, ready.Choice);
        Assert.False(ready.Enabled);
    }

    [Fact]
    public void AFinishedMatchOffersARematchOrTheMainMenu()
    {
        using LobbyFlow flow = InMatch(new StubLobby());

        Assert.Equal(string.Empty, Text(flow, MenuText.Empty));
        Assert.Empty(LobbyMenuItems.For(flow));

        flow.MatchEnded();
        string text = Text(flow, MenuText.Empty);

        Assert.Equal(["Rematch", "Main menu"], Labels(flow));
        Assert.Equal(LobbyChoice.MainMenu, LobbyMenuItems.For(flow)[1].Choice);
        Assert.Contains("Rival: not yet", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AMatchStoppedByADesyncOffersARematchAndStillShowsTheDesync()
    {
        using LobbyFlow flow = InMatch(new StubLobby());
        flow.MatchEnded();

        string text = Text(flow, MenuText.Empty, "DESYNC — match stopped\nChecksums differ.");

        Assert.Equal(["Rematch", "Main menu"], Labels(flow));
        Assert.Contains("DESYNC — match stopped\nChecksums differ.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ADisconnectedMatchReturnsToTheMenuSayingWhy()
    {
        using LobbyFlow flow = InMatch(new StubLobby());

        flow.OpponentDisconnected();
        string text = Text(flow, MenuText.Empty);

        Assert.Contains("Host a lobby", Labels(flow));
        Assert.DoesNotContain("Rematch", Labels(flow));
        Assert.Contains("Lost the connection to Rival: the match ended without a result.", text, StringComparison.Ordinal);
    }

    private static string[] Labels(LobbyFlow flow) =>
        [.. LobbyMenuItems.For(flow).Select(item => MenuTextTable.Formatter.Format(item.Label))];

    private static string Text(LobbyFlow flow, MenuText lastAction, string matchNote = "") =>
        MenuTextTable.Formatter.FormatLines(LobbyMenuText.Describe(flow, lastAction, matchNote));

    private static LobbyFlow InMatch(StubLobby lobby)
    {
        LobbyFlow flow = Hosting(lobby);
        lobby.Opponent = _rival;
        lobby.OpponentReadyFor = 1;
        flow.Update();
        flow.Ready();
        flow.TakeMatchToStart();
        return flow;
    }

    private static LobbyFlow Hosting(StubLobby lobby)
    {
        var flow = new LobbyFlow(lobby);
        flow.Host();
        lobby.Presence = LobbyPresence.Inside;
        lobby.IsOwner = true;
        flow.Update();
        return flow;
    }
}
