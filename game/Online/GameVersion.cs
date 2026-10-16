using System.Reflection;

namespace FightingGame.Online;

/// <summary>This build's version for the online handshake: the .NET SDK writes it as "1.0.0+&lt;git commit&gt;".</summary>
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
