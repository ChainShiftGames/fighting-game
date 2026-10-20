using System;
using System.Globalization;
using System.Net;
using Netcode.Online;
using Netcode.Transport;
using Sim.Data.Online;

namespace FightingGame.Online;

/// <summary>
/// Reads online test options from the command line (developer settings). After <c>--</c>:
/// <list type="bullet">
/// <item><c>--online --port 7000 --remote 127.0.0.1:7001 --player 1</c>: direct UDP (development only).</item>
/// <item>For it and for Steam matches started from the Online menu:
/// <c>[--lag | --ping ms --jitter ms --loss %] [--rollback frames] [--delay frames]</c>.</item>
/// </list>
/// <c>--delay</c> stands in for the player's input delay choice (0–4) for one run; the rest are developer settings.
/// </summary>
public static class OnlineLaunchOptions
{
    public const string Usage =
        "--online --port <local port> --remote <ip:port> --player <1|2>, then optionally " +
        "--lag, --ping <ms>, --jitter <ms>, --loss <percent>, --rollback <frames>, --delay <frames>";

    private const int MaxPacketLossPercent = 100;

    /// <summary>The opponent of a direct UDP match; null with no error when the arguments don't ask for one.</summary>
    /// <remarks>The tuning options are read when the match starts (<see cref="WithTuning"/>, <see cref="InputDelay"/>).</remarks>
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
                remote, remote.ToString(), PlayerOption(args), PeerTransport.DirectUdp(), IntOption(args, "--port", null));
        }
        catch (FormatException exception)
        {
            error = $"{exception.Message} Usage: {Usage}";
            return null;
        }
    }

    /// <summary>
    /// Applies the rollback option to any online match. Developer setting: it overrides the data's value for one run,
    /// as exported test builds can't edit data. The delay option is read by <see cref="InputDelay"/>.
    /// </summary>
    /// <exception cref="FormatException">An option has an invalid value.</exception>
    public static PeerMatchSettings WithTuning(PeerMatchSettings settings, string[] args) => settings with
    {
        RollbackFrames = IntOption(args, "--rollback", settings.RollbackFrames),
    };

    /// <summary>
    /// The network simulation the options ask for (developer settings): <c>--lag</c> starts from ADR-0005's test
    /// conditions, <c>--ping</c>, <c>--jitter</c> and <c>--loss</c> set single values; none without options.
    /// </summary>
    /// <exception cref="FormatException">An option has an invalid value.</exception>
    public static NetworkSimulation Simulation(string[] args)
    {
        NetworkSimulation simulation = Array.IndexOf(args, "--lag") >= 0
            ? NetworkSimulation.TestConditions
            : NetworkSimulation.None;
        int loss = IntOption(args, "--loss", simulation.PacketLossPercent);
        if (loss > MaxPacketLossPercent)
        {
            throw new FormatException($"--loss must be 0 to {MaxPacketLossPercent}, got {loss}.");
        }

        return simulation with
        {
            PingMilliseconds = IntOption(args, "--ping", simulation.PingMilliseconds),
            JitterMilliseconds = IntOption(args, "--jitter", simulation.JitterMilliseconds),
            PacketLossPercent = loss,
        };
    }

    /// <summary>
    /// How this PC picks its input delay: <c>--delay</c> fixes it for this run, else the player's own choice
    /// (<paramref name="playerOverrideFrames"/>, null for automatic) stands.
    /// </summary>
    /// <param name="automatic">How the automatic delay follows the ping (developer settings, from data).</param>
    /// <exception cref="FormatException"><c>--delay</c> isn't a delay a player may choose.</exception>
    public static InputDelaySettings InputDelay(AutomaticInputDelaySettings automatic, int? playerOverrideFrames, string[] args)
    {
        int? overrideFrames = OptionOrNull(args, "--delay") is null ? playerOverrideFrames : IntOption(args, "--delay", null);
        if (overrideFrames is < InputDelaySettings.MinOverrideFrames or > InputDelaySettings.MaxOverrideFrames)
        {
            throw new FormatException(
                $"--delay must be {InputDelaySettings.MinOverrideFrames} to {InputDelaySettings.MaxOverrideFrames}, " +
                $"got {overrideFrames}.");
        }

        return new InputDelaySettings
        {
            Automatic = new InputDelayTuning
            {
                MinimumFrames = automatic.MinimumFrames,
                MaximumFrames = automatic.MaximumFrames,
                PingStepMilliseconds = automatic.PingStepMilliseconds,
                PingSampleIntervalFrames = automatic.PingSampleIntervalFrames,
                PingSamples = automatic.PingSamples,
            },
            PlayerOverrideFrames = overrideFrames,
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
