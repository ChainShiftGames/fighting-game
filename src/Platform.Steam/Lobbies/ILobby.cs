using System;
using System.Collections.Generic;
using Platform.Steam.Friends;

namespace Platform.Steam.Lobbies;

/// <summary>
/// This player's side of a two-player lobby on Steam: membership, ready marks, picks, ping and invites. The thin layer over
/// Steamworks.NET that <see cref="LobbyFlow"/> runs on, so the flow can be tested without Steam.
/// </summary>
public interface ILobby : IDisposable
{
    LobbyPresence Presence { get; }

    /// <summary>Whether this PC owns the lobby it is in. The owner plays P1.</summary>
    bool IsOwner { get; }

    /// <summary>The other member of the lobby; null while there is none.</summary>
    SteamFriend? Opponent { get; }

    /// <summary>The match number the other member marked ready for (<see cref="MarkReadyFor"/>); 0 for none.</summary>
    int OpponentReadyFor { get; }

    /// <summary>The other member's pick mark (<see cref="MarkPick"/>); empty for none.</summary>
    string OpponentPick { get; }

    /// <summary>Round trip to the other member estimated by Steam from both PCs' locations; null while unknown.</summary>
    int? OpponentPingMilliseconds { get; }

    /// <summary>A lobby the player chose to join from a friend's invite in Steam; handed over once, null when none.</summary>
    ulong? TakeAcceptedInvite();

    /// <summary>Why the last create or join failed; handed over once, <see cref="LobbyNotice.None"/> when nothing failed.</summary>
    LobbyNotice TakeFailure();

    /// <summary>Creates a friends-only two-player lobby and enters it.</summary>
    void Create();

    void Join(ulong lobbyId);

    /// <summary>Leaves the lobby, or stops creating or joining one.</summary>
    void Leave();

    /// <summary>Tells the other member this player is ready for match <paramref name="matchNumber"/> (0: not ready).</summary>
    void MarkReadyFor(int matchNumber);

    /// <summary>Tells the other member this player's pick (opaque text; empty: none).</summary>
    void MarkPick(string pick);

    /// <summary>Sends a Steam invite to the lobby. False when not in a lobby or Steam refused.</summary>
    bool Invite(ulong friendSteamId);

    /// <summary>Opens Steam's overlay to invite friends to the lobby. False when not in a lobby or there is no overlay.</summary>
    bool OpenInviteOverlay();

    /// <summary>Friends running this game right now: the ones an invite can reach.</summary>
    IReadOnlyList<SteamFriend> FriendsPlayingThisGame();
}
