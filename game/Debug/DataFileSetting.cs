using System;

namespace FightingGame.Debug;

/// <summary>
/// A whole-number developer setting stored in a <c>data/</c> file (ARCHITECTURE.md §6); the debug panel edits it
/// in place, so the change is versioned with the rest of the data.
/// </summary>
public sealed class DataFileSetting
{
    /// <param name="file">Data-relative path with '/' separators, e.g. <c>rules/match.json</c>.</param>
    /// <param name="key">The field in that file: a top-level key, or a dotted path into nested objects.</param>
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

    /// <summary>The panel's arrow increment, so subpixel distances move in whole pixels; any value can be typed.</summary>
    public int Step { get; }

    /// <summary>
    /// What a missing field means (its loader's default), so the panel shows it and Save adds it; null when the file
    /// must hold the field.
    /// </summary>
    public int? WhenMissing { get; init; }

    /// <exception cref="FormatException">The file doesn't hold the field (or, with <see cref="WhenMissing"/>, its
    /// object) as a whole number.</exception>
    public int Read(string dataRoot)
    {
        string text = System.IO.File.ReadAllText(DataFilePath.In(dataRoot, File));
        return WhenMissing is { } missing ? JsonNumberField.ReadOr(text, Key, missing) : JsonNumberField.Read(text, Key);
    }

    /// <exception cref="ArgumentOutOfRangeException">The value is outside <see cref="Min"/>..<see cref="Max"/>.</exception>
    public void Write(string dataRoot, int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Max);
        string path = DataFilePath.In(dataRoot, File);
        // An unchanged value leaves the file as it is, so a missing field isn't added just by saving the panel.
        if (Read(dataRoot) == value)
        {
            return;
        }

        string text = System.IO.File.ReadAllText(path);
        System.IO.File.WriteAllText(path, WhenMissing is null
            ? JsonNumberField.Write(text, Key, value)
            : JsonNumberField.WriteOrAdd(text, Key, value));
    }
}
