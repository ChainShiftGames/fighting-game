using System.Collections.Generic;

namespace Sim.Data.Sources;

/// <summary>Read-only access to the game data files, by path relative to the data root.</summary>
/// <remarks>
/// Paths always use '/' and are case-sensitive, on every platform, so data that loads on Windows also loads on
/// Linux and the Steam Deck. Listings are sorted ordinally so load order never depends on the file system.
/// </remarks>
public interface IDataSource
{
    bool FileExists(string path);

    /// <summary>The file's raw bytes. Only called for paths that <see cref="FileExists"/> confirmed.</summary>
    byte[] ReadFile(string path);

    /// <summary>Names (not paths) of the files directly inside <paramref name="directory"/>, sorted ordinally.</summary>
    IReadOnlyList<string> ListFiles(string directory);

    /// <summary>Names of the folders directly inside <paramref name="directory"/>, sorted ordinally.</summary>
    IReadOnlyList<string> ListDirectories(string directory);
}
