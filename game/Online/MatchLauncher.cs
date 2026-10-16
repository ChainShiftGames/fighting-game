using System;
using FightingGame.Platform;
using Godot;
using Netcode.Online;

namespace FightingGame.Online;

/// <summary>
/// Picks the match from the command line and hands it over once it can start: at once for local and direct UDP
/// matches, after matchmaking found an opponent for Steam ones.
/// </summary>
public sealed class MatchLauncher : IDisposable
{
    private readonly ToyMatchStartup _startup;
    private readonly string[] _args;
    private StartedMatch? _readyMatch;
    private bool _matchmakingMatchStarted;

    /// <param name="createLocal">Builds the local match; null when it can't (the host shows why).</param>
    /// <param name="startup">Builds online matches.</param>
    public MatchLauncher(
        Func<StartedMatch?> createLocal, ToyMatchStartup startup, IPlatformServices platform, string[] args)
    {
        _startup = startup;
        _args = args;
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

        PeerMatchSettings? direct = OnlineLaunchOptions.ParseDirect(args, out string error);
        if (error.Length > 0)
        {
            GD.PushError($"Ignoring online options, starting a local match: {error}");
        }

        _readyMatch = direct is null
            ? createLocal()
            : startup.CreateOnline(direct, PeerTransport.DirectUdp, direct.RemoteEndPoint.ToString() ?? "?");
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
        var settings = new PeerMatchSettings { RemoteEndPoint = found.Opponent, LocalIsPlayer1 = found.LocalIsPlayer1 };
        try
        {
            settings = OnlineLaunchOptions.WithTuning(settings, _args);
        }
        catch (FormatException exception)
        {
            GD.PushError($"Ignoring invalid online options: {exception.Message}");
        }

        return _startup.CreateOnline(settings, found.Transport, found.OpponentName);
    }

    public void Dispose() => Matchmaking?.Dispose();
}
