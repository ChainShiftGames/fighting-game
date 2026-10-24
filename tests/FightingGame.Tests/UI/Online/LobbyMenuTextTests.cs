using System;
using System.Linq;
using FightingGame.Tests.UI.Framework;
using FightingGame.UI.Framework;
using FightingGame.UI.Online;
using Platform.Steam.Friends;
using Platform.Steam.Lobbies;
using Xunit;

namespace FightingGame.Tests.UI.Online;

public sealed class LobbyMenuTextTests
{
    private const string Me = "Me";
    private static readonly SteamFriend _rival = new(42, "Rival");

    [Fact]
    public void TheOnlineMenuOffersAPrivateLobbyFriendsLobbiesAndQuickMatchForLater()
    {
        var lobby = new StubLobby();
        lobby.Friends.Add(_rival);
        lobby.Friends.Add(new SteamFriend(43, "Host", LobbyId: 900));
        using var flow = new LobbyFlow(lobby);
        flow.RefreshFriends();

        string text = Text(flow, MenuText.Empty);

        Assert.Contains("Play a friend", text, StringComparison.Ordinal);
        Assert.Equal(["Host a private lobby", "Join Host's lobby", "Quick match", "Back"], Labels(flow));
        Assert.Equal(1, LobbyMenuItems.For(flow)[1].FriendIndex);
        LobbyMenuItem quickMatch = LobbyMenuItems.For(flow)[2];
        Assert.False(quickMatch.Enabled);
        Assert.Equal("ONLINE_QUICK_MATCH_INFO", quickMatch.Description.Value);
    }

    [Fact]
    public void WaitingForAnOpponentListsYouAndInvites()
    {
        var lobby = new StubLobby();
        lobby.Friends.Add(_rival);
        using LobbyFlow flow = Hosting(lobby);
        flow.RefreshFriends();

        string text = Text(flow, MenuText.Key("LOBBY_INVITE_SENT", "Rival"));

        Assert.Equal(["Invite through the Steam overlay", "Invite Rival", "Leave the lobby"], Labels(flow));
        Assert.StartsWith("P1  Me (you): in the lobby", text, StringComparison.Ordinal);
        Assert.Contains("waiting for an opponent", text, StringComparison.Ordinal);
        Assert.EndsWith("Invite sent to Rival.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WithAnOpponentShowsBothSidesWhoIsReadyAndTheConnection()
    {
        var lobby = new StubLobby { OpponentPingMilliseconds = 42 };
        using LobbyFlow flow = Hosting(lobby);
        lobby.Opponent = _rival;
        lobby.OpponentReadyFor = 1;
        flow.Update();

        string text = Text(flow, MenuText.Empty);

        Assert.StartsWith("P1  Me (you): not yet\nP2  Rival: ready\n\nConnection: ■■■ 42 ms (good)", text, StringComparison.Ordinal);
        Assert.Equal(["Ready", "Leave the lobby"], Labels(flow));
    }

    [Fact]
    public void TheGuestIsListedSecondAndTheConnectionIsMeasuredFirst()
    {
        var lobby = new StubLobby { Presence = LobbyPresence.Inside, Opponent = _rival };
        using var flow = new LobbyFlow(lobby);
        flow.Update();

        string text = Text(flow, MenuText.Empty);

        Assert.StartsWith("P1  Rival: not yet\nP2  Me (you): not yet\n\nConnection: measuring…", text, StringComparison.Ordinal);
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
        Assert.Equal("Ready — waiting", MenuTextTable.Formatter.Format(ready.Label));
    }

    [Fact]
    public void BothReadyGoesOnToCharacterSelectWithNothingToPress()
    {
        var lobby = new StubLobby();
        using LobbyFlow flow = Hosting(lobby);
        lobby.Opponent = _rival;
        lobby.OpponentReadyFor = 1;
        flow.Update();
        flow.Ready();

        Assert.Equal(LobbyStage.Picking, flow.Stage);
        Assert.Contains("on to character select", Text(flow, MenuText.Empty), StringComparison.Ordinal);
        Assert.Empty(LobbyMenuItems.For(flow));
    }

    [Fact]
    public void TheOpponentLeavingSaysSo()
    {
        var lobby = new StubLobby();
        using LobbyFlow flow = Hosting(lobby);
        lobby.Opponent = _rival;
        flow.Update();

        lobby.Opponent = null;
        flow.Update();

        Assert.EndsWith("Rival left the lobby.", Text(flow, MenuText.Empty), StringComparison.Ordinal);
    }

    [Fact]
    public void ADisconnectedMatchReturnsToTheOnlineMenuSayingWhy()
    {
        using LobbyFlow flow = InMatch(new StubLobby());

        flow.OpponentDisconnected();
        string text = Text(flow, MenuText.Empty);

        Assert.Contains("Host a private lobby", Labels(flow));
        Assert.EndsWith(
            "Connection lost: Rival stopped answering, so the match ended without a result.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AMatchThatFailedToStartSaysWhyAndOffersToTryAgain()
    {
        using LobbyFlow flow = InMatch(new StubLobby());
        flow.MatchEnded();

        string text = Text(flow, MenuText.Empty, "No online match:\nThe other player picked 'nobody'.");

        Assert.Equal(["Rematch", "Leave the lobby"], Labels(flow));
        Assert.Contains("No online match:\nThe other player picked 'nobody'.", text, StringComparison.Ordinal);
    }

    private static string[] Labels(LobbyFlow flow) =>
        [.. LobbyMenuItems.For(flow).Select(item => MenuTextTable.Formatter.Format(item.Label))];

    private static string Text(LobbyFlow flow, MenuText lastAction, string matchNote = "") =>
        MenuTextTable.Formatter.FormatLines(LobbyMenuText.Describe(flow, Me, lastAction, matchNote));

    private static LobbyFlow InMatch(StubLobby lobby)
    {
        LobbyFlow flow = Hosting(lobby);
        lobby.Opponent = _rival;
        lobby.OpponentReadyFor = 1;
        flow.Update();
        flow.Ready();
        flow.Pick("0/ryu");
        lobby.OpponentPick = "1:0/ken";
        flow.Update();
        Assert.NotNull(flow.TakeMatchToStart());
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
