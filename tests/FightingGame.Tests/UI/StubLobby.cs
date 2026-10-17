using System.Collections.Generic;
using Platform.Steam.Friends;
using Platform.Steam.Lobbies;

namespace FightingGame.Tests.UI;

/// <summary>A Steam lobby that is whatever the test sets, so the menu text can be checked at every stage.</summary>
internal sealed class StubLobby : ILobby
{
    public LobbyPresence Presence { get; set; }

    public bool IsOwner { get; set; }

    public SteamFriend? Opponent { get; set; }

    public int OpponentReadyFor { get; set; }

    public List<SteamFriend> Friends { get; } = [];

    public ulong? TakeAcceptedInvite() => null;

    public string TakeFailure() => string.Empty;

    public void Create() => Presence = LobbyPresence.Entering;

    public void Join(ulong lobbyId) => Presence = LobbyPresence.Entering;

    public void Leave() => Presence = LobbyPresence.Outside;

    public void MarkReadyFor(int matchNumber)
    {
    }

    public bool Invite(ulong friendSteamId) => true;

    public bool OpenInviteOverlay() => true;

    public IReadOnlyList<SteamFriend> FriendsPlayingThisGame() => Friends;

    public void Dispose()
    {
    }
}
