using System;
using FightingGame.Platform;
using FightingGame.Training;
using Godot;
using Platform.Steam.Lobbies;

namespace FightingGame.Online;

/// <summary>
/// Starts the match the player picks (<see cref="LaunchMode"/>) and hands it over once it can start: at once on this
/// PC, whenever the lobby has both players ready for Steam ones (the first match and every rematch). The command line
/// can pick one at startup instead of the menus (development): a replay, direct UDP or <c>--training</c>. Steam
/// matches always start from the main menu's Online entry. The menus' flow decides when (<see cref="IMatchStarter"/>).
/// </summary>
/// <remarks>
/// The platform's matchmaking stays open the whole time, so a friend's invite accepted in Steam goes to the online
/// lobby from anywhere: the main menu, or ending a match on this PC.
/// </remarks>
public sealed class MatchLauncher : IMatchStarter, IDisposable
{
    private readonly Func<MatchSetup, StartedMatch?> _createVersus;
    private readonly Func<MatchSetup, StartedMatch?> _createTraining;
    private readonly Func<OnlineOpponent, StartedMatch?> _createOnline;
    private Func<StartedMatch?>? _restart;
    private bool _restartRequested;
    private StartedMatch? _readyMatch;

    /// <param name="createVersus">Builds the local two-player match picked; null when it can't (the host shows why).</param>
    /// <param name="createTraining">Builds the training match picked; null when it can't.</param>
    /// <param name="createOnline">Builds an online match against the opponent; null when it can't.</param>
    /// <param name="createReplay">Plays the replay file at the path back; null when it can't.</param>
    public MatchLauncher(
        Func<MatchSetup, StartedMatch?> createVersus,
        Func<MatchSetup, StartedMatch?> createTraining,
        Func<OnlineOpponent, StartedMatch?> createOnline,
        Func<string, StartedMatch?> createReplay,
        IPlatformServices platform,
        string[] args)
    {
        _createVersus = createVersus;
        _createTraining = createTraining;
        _createOnline = createOnline;
        Matchmaking = platform.OpenMatchmaking();
        PlatformName = platform.PlatformName;
        PlayerName = platform.PlayerName;
        string? replay = ReplayLaunchOptions.ParsePath(args, out string replayError);
        if (replayError.Length > 0)
        {
            GD.PushError($"Ignoring the replay option: {replayError}");
        }

        if (replay is not null)
        {
            Play(LaunchMode.Replay, () => createReplay(replay));
        }
        else if (OnlineLaunchOptions.ParseDirect(args, out string error) is { } direct)
        {
            Mode = LaunchMode.DirectOnline;
            _readyMatch = createOnline(direct);
        }
        else
        {
            if (error.Length > 0)
            {
                GD.PushError($"Ignoring online options: {error}");
            }

            if (TrainingLaunchOptions.WantsTraining(args))
            {
                Start(MatchSetup.For(LaunchMode.Training));
            }
        }
    }

    public event Action? OnlineJoined;

    public LaunchMode Mode { get; private set; } = LaunchMode.MainMenu;

    /// <summary>Online through the platform; null when it has none (offline), so the online menu can't open.</summary>
    public IMatchmaking? Matchmaking { get; }

    /// <summary>The platform's name for the player, e.g. why online play isn't there ("Offline").</summary>
    public string PlatformName { get; }

    /// <summary>This player's name on the platform, as the lobby shows it.</summary>
    public string PlayerName { get; }

    /// <summary>After an online match: this player wants the rematch and the other hasn't said yet.</summary>
    public bool IsWaitingForRematch =>
        Mode == LaunchMode.Online && Matchmaking?.Lobby is { Stage: LobbyStage.Rematch, IsLocalReady: true };

    /// <summary>Whether a finished match can be played again: a new match on this PC, or a rematch in the lobby.</summary>
    public bool CanRematch => Mode is LaunchMode.Online || _restart is not null;

    /// <summary>Whether a match or the lobby is on this PC only, so it can start over (rematch, data saved).</summary>
    public bool IsLocal => Mode is LaunchMode.Versus or LaunchMode.Training or LaunchMode.Replay;

    public void Start(MatchSetup setup)
    {
        Func<MatchSetup, StartedMatch?> create = setup.Mode switch
        {
            LaunchMode.Versus => _createVersus,
            LaunchMode.Training => _createTraining,
            _ => throw new ArgumentException($"{setup.Mode} matches don't start from a setup.", nameof(setup)),
        };
        Play(setup.Mode, () => create(setup));
    }

    public bool OpenOnline()
    {
        if (Matchmaking is null)
        {
            return false;
        }

        Leave();
        Mode = LaunchMode.Online;
        return true;
    }

    /// <summary>
    /// After a finished match: plays the same setup again on this PC, or marks this player ready for a rematch in the
    /// lobby (it starts once both are).
    /// </summary>
    public void Rematch()
    {
        if (Mode == LaunchMode.Online)
        {
            Matchmaking!.Lobby.Ready();
        }
        else
        {
            Restart();
        }
    }

    /// <summary>Starts the match on this PC over (also after its data was saved). False for online modes.</summary>
    public bool Restart()
    {
        if (!IsLocal || _restart is null)
        {
            return false;
        }

        _restartRequested = true;
        return true;
    }

    public void Stop()
    {
        Leave();
        Mode = LaunchMode.MainMenu;
    }

    /// <summary>The match to play once it can start, handed over once; null until then.</summary>
    public StartedMatch? TakeReadyMatch()
    {
        if (_restartRequested && _restart is not null)
        {
            _restartRequested = false;
            return _restart();
        }

        if (_readyMatch is { } ready)
        {
            _readyMatch = null;
            return ready;
        }

        if (Mode != LaunchMode.Online || Matchmaking?.TakeMatchToStart() is not { } found)
        {
            return null;
        }

        StartedMatch? match = _createOnline(
            new OnlineOpponent(found.Opponent, found.OpponentName, found.LocalIsPlayer1, found.Transport, LocalPort: 0)
                .WithPicks(found.LocalPick, found.OpponentPick));
        if (match is null)
        {
            // The host shows why; the lobby offers to try again.
            Matchmaking.Lobby.MatchEnded();
        }

        return match;
    }

    /// <summary>
    /// Brings the lobby up to date, once per displayed frame before <see cref="TakeReadyMatch"/>. Returns true when the
    /// running match must end: the player picked something else (restart, main menu, a friend's invite), or the lobby
    /// moved on (a rematch is starting, the opponent left or disconnected, or so did we).
    /// </summary>
    public bool EndsMatch(StartedMatch? current)
    {
        LobbyFlow? lobby = Matchmaking?.Lobby;
        lobby?.Update();
        if (lobby is not null && Mode != LaunchMode.Online && lobby.Stage != LobbyStage.Idle)
        {
            // The player accepted a friend's invite in Steam: they chose to go.
            Mode = LaunchMode.Online;
            _restart = null;
            _restartRequested = false;
            OnlineJoined?.Invoke();
        }

        if (current is null)
        {
            return false;
        }

        if (Mode == LaunchMode.MainMenu || _restartRequested)
        {
            return true;
        }

        if (Mode != LaunchMode.Online || lobby is null)
        {
            return false;
        }

        if (current is { OpponentDisconnected: true })
        {
            // No rematch with an opponent whose PC is gone: back to the lobby menu, which says why.
            lobby.OpponentDisconnected();
        }
        else if (current is { IsOver: true } && lobby.Stage == LobbyStage.InMatch)
        {
            lobby.MatchEnded();
        }

        return lobby.Stage is not (LobbyStage.InMatch or LobbyStage.Rematch);
    }

    public void Dispose() => Matchmaking?.Dispose();

    private void Play(LaunchMode mode, Func<StartedMatch?> create)
    {
        Leave();
        Mode = mode;
        _restart = create;
        _restartRequested = true;
    }

    // Whatever was going on stops: the lobby is left, nothing restarts.
    private void Leave()
    {
        Matchmaking?.Lobby.Leave();
        _restart = null;
        _restartRequested = false;
        _readyMatch = null;
    }
}
