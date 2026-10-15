using System;
using System.Collections.Generic;

namespace FightingGame.Platform;

/// <summary>Finds an online opponent through the platform: invite a friend, or accept a friend's invite.</summary>
public interface IMatchmaking : IDisposable
{
    /// <summary>Whether invites can be sent yet.</summary>
    bool IsReady { get; }

    /// <summary>Friends who can be invited now, as of the last <see cref="RefreshFriends"/>.</summary>
    IReadOnlyList<string> InvitableFriends { get; }

    /// <summary>The opponent and how to reach them, once someone has joined.</summary>
    MatchmakingResult? Result { get; }

    void RefreshFriends();

    /// <summary>Invites the friend at this index of <see cref="InvitableFriends"/>.</summary>
    void Invite(int friendIndex);
}
