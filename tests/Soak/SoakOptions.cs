using System;
using System.Globalization;
using Netcode.Bots;

namespace Soak;

/// <summary>Command-line options of the soak test. All are developer settings (ARCHITECTURE.md §6).</summary>
internal sealed record SoakOptions
{
    public static readonly string Usage =
        "Usage: Soak [--minutes <n>] [--seed <n>] [--bot-reaction <frames>] [--dump-dir <path>]\n" +
        "  --minutes       How long to play matches (developer setting). Default 60: the soak of ADR-0006.\n" +
        "  --seed          Seeds every match's bots (developer setting). Default: a new random seed, printed.\n" +
        $"  --bot-reaction  Both bots' reaction delay in frames, 0 to {BotSettings.MaxReactionDelayFrames} " +
        $"(developer setting). Default {BotSettings.DefaultReactionDelayFrames}.\n" +
        "  --dump-dir      Where a desync dump is written. Default: soak-dumps.";

    private const double DefaultMinutes = 60;
    private const string DefaultDumpDirectory = "soak-dumps";

    /// <summary>How long the soak plays matches back to back (developer setting).</summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromMinutes(DefaultMinutes);

    /// <summary>
    /// Seeds the bots of every match (developer setting). Null: pick one at random, so each run tries new inputs;
    /// the run prints it either way so a failure can be replayed.
    /// </summary>
    public uint? Seed { get; init; }

    /// <summary>How both CPU bots play; the command line sets their reaction delay (developer setting).</summary>
    public BotSettings Bots { get; init; } = new();

    public string DumpDirectory { get; init; } = DefaultDumpDirectory;

    public static bool TryParse(string[] args, out SoakOptions options, out string error)
    {
        options = new SoakOptions();
        error = string.Empty;
        for (int i = 0; i < args.Length; i += 2)
        {
            string name = args[i];
            if (i + 1 >= args.Length)
            {
                error = $"'{name}' needs a value.";
                return false;
            }

            string value = args[i + 1];
            switch (name)
            {
                case "--minutes" when double.TryParse(value, CultureInfo.InvariantCulture, out double minutes)
                    && minutes > 0:
                    options = options with { Duration = TimeSpan.FromMinutes(minutes) };
                    break;
                case "--seed" when uint.TryParse(value, CultureInfo.InvariantCulture, out uint seed):
                    options = options with { Seed = seed };
                    break;
                case "--bot-reaction" when int.TryParse(value, CultureInfo.InvariantCulture, out int frames)
                    && frames >= 0 && frames <= BotSettings.MaxReactionDelayFrames:
                    options = options with { Bots = options.Bots with { ReactionDelayFrames = frames } };
                    break;
                case "--dump-dir" when value.Length > 0:
                    options = options with { DumpDirectory = value };
                    break;
                case "--minutes" or "--seed" or "--bot-reaction" or "--dump-dir":
                    error = $"Invalid value '{value}' for '{name}'.";
                    return false;
                default:
                    error = $"Unknown option '{name}'.";
                    return false;
            }
        }

        return true;
    }
}
