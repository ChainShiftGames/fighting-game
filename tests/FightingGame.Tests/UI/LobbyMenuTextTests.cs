using System;
using FightingGame.UI;
using Platform.Steam.Friends;
using Platform.Steam.Lobbies;
using Xunit;

namespace FightingGame.Tests.UI;

public sealed class LobbyMenuTextTests
{
    private static readonly SteamFriend _rival = new(42, "Rival");

    [Fact]
    public void IdleOffersToHost()
    {
        using var flow = new LobbyFlow(new StubLobby());

        string text = LobbyMenuText.Describe(flow, string.Empty);

        Assert.Contains("H   host a lobby", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Esc", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WaitingForAnOpponentListsInvites()
    {
        var lobby = new StubLobby();
        lobby.Friends.Add(_rival);
        using LobbyFlow flow = Hosting(lobby);
        flow.RefreshFriends();

        string text = LobbyMenuText.Describe(flow, "Invite sent to Rival.");

        Assert.Contains("I   invite a friend through the Steam overlay", text, StringComparison.Ordinal);
        Assert.Contains("1   invite Rival", text, StringComparison.Ordinal);
        Assert.Contains("Esc   leave the lobby", text, StringComparison.Ordinal);
        Assert.EndsWith("Invite sent to Rival.\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WithAnOpponentShowsPortsAndWhoIsReady()
    {
        var lobby = new StubLobby();
        using LobbyFlow flow = Hosting(lobby);
        lobby.Opponent = _rival;
        lobby.OpponentReadyFor = 1;
        flow.Update();

        string text = LobbyMenuText.Describe(flow, string.Empty);

        Assert.Contains("you (P1) vs Rival (P2)", text, StringComparison.Ordinal);
        Assert.Contains("You: not yet — Enter   ready", text, StringComparison.Ordinal);
        Assert.Contains("Rival: ready", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AFinishedMatchOffersARematch()
    {
        var lobby = new StubLobby();
        using LobbyFlow flow = Hosting(lobby);
        lobby.Opponent = _rival;
        lobby.OpponentReadyFor = 1;
        flow.Update();
        flow.Ready();
        flow.TakeMatchToStart();

        Assert.Equal(string.Empty, LobbyMenuText.Describe(flow, string.Empty));

        flow.MatchEnded();
        string text = LobbyMenuText.Describe(flow, string.Empty);

        Assert.Contains("Match over", text, StringComparison.Ordinal);
        Assert.Contains("Enter   rematch", text, StringComparison.Ordinal);
        Assert.Contains("Rival: not yet", text, StringComparison.Ordinal);
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
