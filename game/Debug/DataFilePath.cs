using System.IO;

namespace FightingGame.Debug;

/// <summary>Where a data-relative file (<c>rules/match.json</c>, '/' separators) lives under the data folder.</summary>
public static class DataFilePath
{
    public static string In(string dataRoot, string file) =>
        Path.Combine(dataRoot, file.Replace('/', Path.DirectorySeparatorChar));
}
