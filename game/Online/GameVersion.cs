using System.Reflection;

namespace FightingGame.Online;

/// <summary>
/// This build's version for the online handshake and replays: the release version (ADR-0044) plus the commit, which
/// the .NET SDK writes as "0.1.0+&lt;git commit&gt;".
/// </summary>
/// <remarks>
/// The commit makes any two different builds refuse each other, as different code may desync (ADR-0005 §6).
/// </remarks>
public static class GameVersion
{
    private const string Unknown = "unknown";

    public static string Current { get; } =
        typeof(GameVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Unknown;
}
