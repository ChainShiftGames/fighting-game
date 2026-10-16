using System;
using System.Globalization;
using System.Net;
using Netcode.Online;
using Netcode.Transport;

namespace FightingGame.Online;

/// <summary>
/// Reads an online test match from the command line (developer settings, until there is a menu). After <c>--</c>:
/// <list type="bullet">
/// <item><c>--steam</c>: find the opponent through a Steam lobby and invites, play over Steam's relay.</item>
/// <item><c>--online --port 7000 --remote 127.0.0.1:7001 --player 1</c>: direct UDP (development only).</item>
/// <item>Either one: <c>[--lag | --ping ms --jitter ms --loss %] [--rollback frames] [--delay frames]</c>.</item>
/// </list>
/// </summary>
public static class OnlineLaunchOptions
{
    public const string Usage =
        "--steam | --online --port <local port> --remote <ip:port> --player <1|2>, then optionally " +
        "--lag, --ping <ms>, --jitter <ms>, --loss <percent>, --rollback <frames>, --delay <frames>";

    public static bool WantsSteam(string[] args) => Array.IndexOf(args, "--steam") >= 0;

    /// <summary>The opponent of a direct UDP match; null with no error when the arguments don't ask for one.</summary>
    /// <remarks>The tuning options are read when the match starts (<see cref="WithTuning"/>).</remarks>
    public static OnlineOpponent? ParseDirect(string[] args, out string error)
    {
        error = string.Empty;
        if (Array.IndexOf(args, "--online") < 0)
        {
            return null;
        }

        try
        {
            var remote = IPEndPoint.Parse(Option(args, "--remote"));
            return new OnlineOpponent(
                remote, remote.ToString(), PlayerOption(args), PeerTransport.DirectUdp, IntOption(args, "--port", null));
        }
        catch (FormatException exception)
        {
            error = $"{exception.Message} Usage: {Usage}";
            return null;
        }
    }

    /// <summary>
    /// Applies the rollback, delay and network-simulation options to any online match. Developer settings: they
    /// override the data's values for one run, as exported test builds can't edit data.
    /// </summary>
    /// <exception cref="FormatException">An option has an invalid value.</exception>
    public static PeerMatchSettings WithTuning(PeerMatchSettings settings, string[] args)
    {
        NetworkSimulation simulation = Array.IndexOf(args, "--lag") >= 0
            ? NetworkSimulation.TestConditions
            : NetworkSimulation.None;
        return settings with
        {
            RollbackFrames = IntOption(args, "--rollback", settings.RollbackFrames),
            InputDelayFrames = IntOption(args, "--delay", settings.InputDelayFrames),
            NetworkSimulation = simulation with
            {
                PingMilliseconds = IntOption(args, "--ping", simulation.PingMilliseconds),
                JitterMilliseconds = IntOption(args, "--jitter", simulation.JitterMilliseconds),
                PacketLossPercent = IntOption(args, "--loss", simulation.PacketLossPercent),
            },
        };
    }

    private static bool PlayerOption(string[] args) => IntOption(args, "--player", null) switch
    {
        1 => true,
        2 => false,
        _ => throw new FormatException("--player must be 1 or 2."),
    };

    private static int IntOption(string[] args, string name, int? fallback)
    {
        string? text = OptionOrNull(args, name);
        if (text is null)
        {
            return fallback ?? throw new FormatException($"{name} is required.");
        }

        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
            ? value
            : throw new FormatException($"{name} must be a whole number, got '{text}'.");
    }

    private static string Option(string[] args, string name) =>
        OptionOrNull(args, name) ?? throw new FormatException($"{name} is required.");

    private static string? OptionOrNull(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
