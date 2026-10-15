using System;
using System.Globalization;
using System.Net;
using Netcode.Online;
using Netcode.Transport;

namespace FightingGame.Online;

/// <summary>
/// Reads an online test match from the command line (developer settings, until there is a lobby):
/// <c>-- --online --port 7000 --remote 127.0.0.1:7001 --player 1 [--lag | --ping 100 --jitter 20 --loss 2] [--rollback 8]</c>.
/// </summary>
public static class OnlineLaunchOptions
{
    public const string Usage =
        "--online --port <local port> --remote <ip:port> --player <1|2> [--lag] [--ping <ms>] [--jitter <ms>] " +
        "[--loss <percent>] [--rollback <frames>] [--delay <frames>]";

    /// <summary>Null settings and no error when the arguments don't ask for an online match.</summary>
    public static PeerMatchSettings? Parse(string[] args, out string error)
    {
        error = string.Empty;
        if (Array.IndexOf(args, "--online") < 0)
        {
            return null;
        }

        try
        {
            NetworkSimulation simulation = Array.IndexOf(args, "--lag") >= 0
                ? NetworkSimulation.TestConditions
                : NetworkSimulation.None;
            simulation = simulation with
            {
                PingMilliseconds = IntOption(args, "--ping", simulation.PingMilliseconds),
                JitterMilliseconds = IntOption(args, "--jitter", simulation.JitterMilliseconds),
                PacketLossPercent = IntOption(args, "--loss", simulation.PacketLossPercent),
            };

            var settings = new PeerMatchSettings
            {
                LocalPort = IntOption(args, "--port", null),
                RemoteEndPoint = IPEndPoint.Parse(Option(args, "--remote")),
                LocalIsPlayer1 = PlayerOption(args),
                NetworkSimulation = simulation,
            };
            return settings with
            {
                RollbackFrames = IntOption(args, "--rollback", settings.RollbackFrames),
                InputDelayFrames = IntOption(args, "--delay", settings.InputDelayFrames),
            };
        }
        catch (FormatException exception)
        {
            error = $"{exception.Message} Usage: {Usage}";
            return null;
        }
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
