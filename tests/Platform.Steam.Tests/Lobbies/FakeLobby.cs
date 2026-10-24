using System.Collections.Generic;
using Platform.Steam.Friends;
using Platform.Steam.Lobbies;

namespace Platform.Steam.Tests.Lobbies;

/// <summary>
/// One PC's Steam lobby as the tests set it: what Steam would report is plain settable state, and every call the flow
/// makes is recorded. Create and join stay <see cref="LobbyPresence.Entering"/> until the test calls <see cref="Enter"/>.
/// </summary>
internal sealed class FakeLobby : ILobby
{
    public LobbyPresence Presence { get; set; } = LobbyPresence.Outside;

    public bool IsOwner { get; set; }

    public SteamFriend? Opponent { get; set; }

    public int OpponentReadyFor { get; set; }

    public string OpponentPick { get; set; } = string.Empty;

    public int? OpponentPingMilliseconds { get; set; }

    public ulong? AcceptedInvite { get; set; }

    public LobbyNotice Failure { get; set; } = LobbyNotice.None;

    public List<SteamFriend> Friends { get; } = [];

    /// <summary>Every ready mark published, in order (0 = not ready).</summary>
    public List<int> ReadyMarks { get; } = [];

    /// <summary>The ready mark the other PC would read now.</summary>
    public int LastReadyMark => ReadyMarks.Count == 0 ? 0 : ReadyMarks[^1];

    /// <summary>Every pick mark published, in order (empty = none).</summary>
    public List<string> PickMarks { get; } = [];

    /// <summary>The pick mark the other PC would read now.</summary>
    public string LastPickMark => PickMarks.Count == 0 ? string.Empty : PickMarks[^1];

    public List<ulong> Invited { get; } = [];

    public List<ulong> Joined { get; } = [];

    public int Creates { get; private set; }

    public int Leaves { get; private set; }

    public bool HasOverlay { get; set; } = true;

    public bool Disposed { get; private set; }

    /// <summary>Steam put this PC in the lobby it was creating or joining.</summary>
    public void Enter(bool asOwner)
    {
        Presence = LobbyPresence.Inside;
        IsOwner = asOwner;
    }

    public ulong? TakeAcceptedInvite()
    {
        ulong? invite = AcceptedInvite;
        AcceptedInvite = null;
        return invite;
    }

    public LobbyNotice TakeFailure()
    {
        LobbyNotice failure = Failure;
        Failure = LobbyNotice.None;
        return failure;
    }

    public void Create()
    {
        Creates++;
        Presence = LobbyPresence.Entering;
    }

    public void Join(ulong lobbyId)
    {
        Joined.Add(lobbyId);
        Presence = LobbyPresence.Entering;
    }

    public void Leave()
    {
        Leaves++;
        Presence = LobbyPresence.Outside;
        IsOwner = false;
        Opponent = null;
        OpponentReadyFor = 0;
        OpponentPick = string.Empty;
    }

    public void MarkReadyFor(int matchNumber) => ReadyMarks.Add(matchNumber);

    public void MarkPick(string pick) => PickMarks.Add(pick);

    public bool Invite(ulong friendSteamId)
    {
        Invited.Add(friendSteamId);
        return Presence == LobbyPresence.Inside;
    }

    public bool OpenInviteOverlay() => Presence == LobbyPresence.Inside && HasOverlay;

    public IReadOnlyList<SteamFriend> FriendsPlayingThisGame() => Friends;

    public void Dispose() => Disposed = true;
}
