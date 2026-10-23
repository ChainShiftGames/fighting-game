using System;

namespace FightingGame.Online;

/// <summary>Starts and stops matches for the menus' flow (<c>UI/Flow/GameFlow</c>).</summary>
public interface IMatchStarter
{
    /// <summary>What plays now; set from the command line before the menus open (training, a replay, direct online).</summary>
    LaunchMode Mode { get; }

    /// <summary>A friend's invite accepted in the platform took the game to the online lobby, from wherever it was.</summary>
    event Action? OnlineJoined;

    /// <summary>Starts a match on this PC (versus or training) with what the menus picked.</summary>
    void Start(MatchSetup setup);

    /// <summary>Goes to the online lobby, leaving whatever runs. False when the platform has no online play.</summary>
    bool OpenOnline();

    /// <summary>After a finished match: plays it again, or marks this player ready in the lobby.</summary>
    void Rematch();

    /// <summary>Ends the running match and leaves the lobby; nothing plays until the next start.</summary>
    void Stop();
}
