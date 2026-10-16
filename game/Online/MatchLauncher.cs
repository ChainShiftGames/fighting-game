using System;
using FightingGame.Platform;
using Godot;

namespace FightingGame.Online;

/// <summary>
/// Picks the match from the command line and hands it over once it can start: at once for local, replay and direct
/// UDP matches, after matchmaking found an opponent for Steam ones.
/// </summary>
public sealed class MatchLauncher : IDisposable
{
    private readonly Func<OnlineOpponent, StartedMatch?> _createOnline;
    private StartedMatch? _readyMatch;
    private bool _matchmakingMatchStarted;

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

        if (_matchmakingMatchStarted || Matchmaking?.Result is not { } found)
        {
            return null;
        }

        _matchmakingMatchStarted = true;
        return _createOnline(
            new OnlineOpponent(found.Opponent, found.OpponentName, found.LocalIsPlayer1, found.Transport, LocalPort: 0));
    }

    public void Dispose() => Matchmaking?.Dispose();
}
