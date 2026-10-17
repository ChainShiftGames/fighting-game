using System;

namespace FightingGame.Online;

/// <summary>
/// Reads a replay to play back from the command line (developer setting, until there is a replay menu). After
/// <c>--</c>: <c>--replay &lt;path&gt;</c>, a replay file's path or a <c>user://</c> path.
/// </summary>
public static class ReplayLaunchOptions
{
    public const string Usage = "--replay <path to a .fgreplay file>";

    private const string Option = "--replay";

    /// <summary>The replay file to play; null with no error when the arguments don't ask for one.</summary>
    public static string? ParsePath(string[] args, out string error)
    {
        error = string.Empty;
        int index = Array.IndexOf(args, Option);
        if (index < 0)
        {
            return null;
        }

        if (index + 1 < args.Length && args[index + 1].Length > 0 && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            return args[index + 1];
        }

        error = $"{Option} needs a file. Usage: {Usage}";
        return null;
    }
}
