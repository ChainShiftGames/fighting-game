using System;
using FightingGame.Platform;
using Godot;
using Platform.Steam.Lobbies;

namespace FightingGame.Online;

/// <summary>
/// Picks the match from the command line and hands it over once it can start: at once for local, replay and direct
/// UDP matches, whenever the lobby has both players ready for Steam ones (the first match and every rematch).
/// </summary>
public sealed class MatchLauncher : IDisposable
{
    private readonly Func<OnlineOpponent, StartedMatch?> _createOnline;
    private StartedMatch? _readyMatch;

    /// <param name="createLocal">Builds the local match; null when it can't (the host shows why).</param>
    /// <param name="createOnline">Builds an online match against the opponent; null when it can't.</param>
    /// <param name="createReplay">Plays the replay file at the path back; null when it can't.</param>
    public MatchLauncher(
        Func<StartedMatch?> createLocal,
        Func<OnlineOpponent, StartedMatch?> createOnline,
        Func<string, StartedMatch?> createReplay,
        IPlatformServices platform,
        string[] args)
    {
        _createOnline = createOnline;
        string? replay = ReplayLaunchOptions.ParsePath(args, out string replayError);
        if (replayError.Length > 0)
        {
            GD.PushError($"Ignoring the replay option: {replayError}");
        }

        if (replay is not null)
        {
            _readyMatch = createReplay(replay);
            return;
        }

        if (OnlineLaunchOptions.WantsSteam(args))
        {
            Matchmaking = platform.OpenMatchmaking();
            if (Matchmaking is null)
            {
                GD.PushError($"--steam needs Steam, which isn't available ({platform.PlatformName}); starting a local match.");
                _readyMatch = createLocal();
            }

            return;
        }

        OnlineOpponent? direct = OnlineLaunchOptions.ParseDirect(args, out string error);
        if (error.Length > 0)
        {
            GD.PushError($"Ignoring online options, starting a local match: {error}");
        }

        _readyMatch = direct is null ? createLocal() : createOnline(direct);
    }

    /// <summary>Set while looking for an opponent through the platform; null for local and direct matches.</summary>
    public IMatchmaking? Matchmaking { get; }

    /// <summary>The match to play once it can start, handed over once; null until then.</summary>
    public StartedMatch? TakeReadyMatch()
    {
        if (_readyMatch is { } ready)
        {
            _readyMatch = null;
            return ready;
        }

        if (Matchmaking?.TakeMatchToStart() is not { } found)
        {
            return null;
        }

        StartedMatch? match = _createOnline(
            new OnlineOpponent(found.Opponent, found.OpponentName, found.LocalIsPlayer1, found.Transport, LocalPort: 0));
        if (match is null)
        {
            // The host shows why; the lobby offers to try again.
            Matchmaking.Lobby.MatchEnded();
        }

        return match;
    }

    /// <summary>
    /// Brings the lobby up to date, once per displayed frame before <see cref="TakeReadyMatch"/>. Returns true when the
    /// running match must end because the lobby moved on: a rematch is starting, the opponent left or disconnected, or
    /// so did we.
    /// </summary>
    public bool EndsMatch(StartedMatch? current)
    {
        if (Matchmaking is null)
        {
            return false;
        }

        LobbyFlow lobby = Matchmaking.Lobby;
        lobby.Update();
        if (current is { OpponentDisconnected: true })
        {
            // No rematch with an opponent whose PC is gone: back to the menu, which says why.
            lobby.OpponentDisconnected();
        }
        else if (current is { IsOver: true } && lobby.Stage == LobbyStage.InMatch)
        {
            lobby.MatchEnded();
        }

        return current is not null && lobby.Stage is not (LobbyStage.InMatch or LobbyStage.Rematch);
    }

    public void Dispose() => Matchmaking?.Dispose();
}
