using System;
using System.IO;

namespace FightingGame.Debug;

/// <summary>
/// A whole-number developer setting stored in a <c>data/</c> file (ARCHITECTURE.md §6); the debug panel edits it
/// in place, so the change is versioned with the rest of the data.
/// </summary>
public sealed class DataFileSetting
{
    /// <param name="file">Data-relative path with '/' separators, e.g. <c>rules/match.json</c>.</param>
    /// <param name="key">The top-level field in that file.</param>
    public DataFileSetting(string name, string file, string key, int min, int max, int step)
    {
        Name = name;
        File = file;
        Key = key;
        Min = min;
        Max = max;
        Step = step;
    }

    public string Name { get; }

    public string File { get; }

    public string Key { get; }

    public int Min { get; }

    public int Max { get; }

    /// <summary>The panel's increment, so subpixel distances move in whole pixels.</summary>
    public int Step { get; }

    /// <exception cref="FormatException">The file doesn't hold the field as a whole number.</exception>
    public int Read(string dataRoot) => JsonNumberField.Read(System.IO.File.ReadAllText(PathIn(dataRoot)), Key);

    /// <exception cref="ArgumentOutOfRangeException">The value is outside <see cref="Min"/>..<see cref="Max"/>.</exception>
    public void Write(string dataRoot, int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Max);
        string path = PathIn(dataRoot);
        System.IO.File.WriteAllText(path, JsonNumberField.Write(System.IO.File.ReadAllText(path), Key, value));
    }

    private string PathIn(string dataRoot) => Path.Combine(dataRoot, File.Replace('/', Path.DirectorySeparatorChar));
}
