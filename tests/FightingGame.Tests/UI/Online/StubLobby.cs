using System.Collections.Generic;
using Platform.Steam.Friends;
using Platform.Steam.Lobbies;

namespace FightingGame.Tests.UI.Online;

/// <summary>A Steam lobby that is whatever the test sets, so the online screens can be checked at every stage.</summary>
internal sealed class StubLobby : ILobby
{
    public LobbyPresence Presence { get; set; }

    public bool IsOwner { get; set; }

    public SteamFriend? Opponent { get; set; }

    public int OpponentReadyFor { get; set; }

    public string OpponentPick { get; set; } = string.Empty;

    public int? OpponentPingMilliseconds { get; set; }

    /// <summary>The pick mark the other PC would read now.</summary>
    public string LastPickMark { get; private set; } = string.Empty;

    public List<SteamFriend> Friends { get; } = [];

    public ulong? TakeAcceptedInvite() => null;

    public LobbyNotice TakeFailure() => LobbyNotice.None;

    public void Create() => Presence = LobbyPresence.Entering;

    public void Join(ulong lobbyId) => Presence = LobbyPresence.Entering;

    public void Leave() => Presence = LobbyPresence.Outside;

    public void MarkReadyFor(int matchNumber)
    {
    }

    public void MarkPick(string pick) => LastPickMark = pick;

    public bool Invite(ulong friendSteamId) => true;

    public bool OpenInviteOverlay() => true;

    public IReadOnlyList<SteamFriend> FriendsPlayingThisGame() => Friends;

    public void Dispose()
    {
    }
}
